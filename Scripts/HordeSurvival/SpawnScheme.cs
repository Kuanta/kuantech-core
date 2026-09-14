using System;
using UnityEngine;

namespace Kuantech.HordeSurvival
{
    /// <summary>
    /// A preferred direction to spawn toward, in world space on the XZ plane. Whoever owns the horde
    /// decides what "preferred" means (ahead of the player, into an empty sector, ...); the scheme only
    /// obeys it. Zero direction or zero weight means "no preference — sample the whole shape".
    /// </summary>
    public struct SpawnHint
    {
        /// <summary>Normalised XZ direction from the focus point. Vector3.zero = no preference.</summary>
        public Vector3 Direction;
        /// <summary>0 = ignore the direction entirely, 1 = commit to it as tightly as SpreadAngle allows.</summary>
        public float Weight;

        public static readonly SpawnHint None = new SpawnHint { Direction = Vector3.zero, Weight = 0f };

        public bool HasDirection => Weight > 0f && Direction.sqrMagnitude > 0.0001f;
    }

    /// <summary>
    /// Strategy for choosing where enemies spawn. Swappable per game / per spawner
    /// (e.g. an annulus around the player, predefined zones, a spawn grid, ...).
    /// Kept intentionally game-agnostic so it can be reused outside the horde flow.
    ///
    /// Obstacle rejection lives here: subclasses only produce raw candidate points via
    /// <see cref="SampleCandidate"/>, and the base retries + validates them against the
    /// obstacle mask, so every scheme avoids spawning inside obstacles for free.
    /// </summary>
    [Serializable]
    public abstract class SpawnScheme
    {
        [Header("Validity")]
        [Tooltip("Physics layers treated as blocking. A candidate overlapping these is rejected. " +
                 "Leave as Nothing to disable obstacle checking.")]
        public LayerMask ObstacleMask;
        [Tooltip("Clearance radius checked around each candidate against the obstacle mask.")]
        public float ClearanceRadius = 0.5f;
        [Tooltip("How many candidates to try before giving up for this spawn.")]
        public int MaxAttempts = 20;

        /// <summary>
        /// Produces a validated spawn point. Repeatedly samples candidates and rejects any that
        /// overlap the obstacle mask, up to <see cref="MaxAttempts"/>. Returns false if none were valid.
        /// <paramref name="focus"/> is the reference point most schemes revolve around (usually the
        /// player position); schemes that ignore it (e.g. fixed zones) may disregard it.
        /// </summary>
        public bool TryGetSpawnPoint(Vector3 focus, out Vector3 position)
        {
            return TryGetSpawnPoint(focus, SpawnHint.None, out position);
        }

        /// <summary>
        /// As above, but biased toward <paramref name="hint"/>. If every biased candidate is blocked the
        /// last attempts fall back to an unbiased sample, so a wall in the preferred direction degrades
        /// into "spawn somewhere valid" rather than "do not spawn at all".
        /// </summary>
        public bool TryGetSpawnPoint(Vector3 focus, SpawnHint hint, out Vector3 position)
        {
            int attempts = Mathf.Max(1, MaxAttempts);
            // Last quarter of the attempts drops the bias — better an unbiased spawn than none.
            int biasedAttempts = hint.HasDirection ? Mathf.Max(1, attempts * 3 / 4) : 0;

            for (int i = 0; i < attempts; i++)
            {
                Vector3 candidate = i < biasedAttempts
                    ? SampleCandidate(focus, hint)
                    : SampleCandidate(focus);

                if (IsPointValid(candidate))
                {
                    position = candidate;
                    return true;
                }
            }
            position = Vector3.zero;
            return false;
        }

        /// <summary>Samples one raw candidate point in world space (no validity check). Scheme-specific.</summary>
        protected abstract Vector3 SampleCandidate(Vector3 focus);

        /// <summary>
        /// Samples a candidate biased toward the hint. Schemes with no meaningful notion of direction
        /// (fixed zones, spawn pads) can leave this alone and simply ignore the hint.
        /// </summary>
        protected virtual Vector3 SampleCandidate(Vector3 focus, in SpawnHint hint)
        {
            return SampleCandidate(focus);
        }

        /// <summary>
        /// True when the point is clear of obstacles. Virtual so a future scheme can swap in a
        /// different notion of validity (e.g. a NavMesh sample check).
        /// </summary>
        public virtual bool IsPointValid(Vector3 position)
        {
            if (ObstacleMask == 0) return true; // no mask configured → accept everything
            return !UnityEngine.Physics.CheckSphere(position, ClearanceRadius, ObstacleMask, QueryTriggerInteraction.Ignore);
        }
    }

    /// <summary>
    /// Spawns on a ring band (annulus) around the focus point — the classic horde-survival
    /// scheme where enemies appear around the player, ideally just off-screen.
    /// </summary>
    [Serializable]
    public class AnnulusSpawnScheme : SpawnScheme
    {
        [Header("Annulus")]
        [Tooltip("Inner radius of the spawn band. Keep it beyond the visible area so enemies appear off-screen.")]
        public float MinRadius = 12f;
        [Tooltip("Outer radius of the spawn band.")]
        public float MaxRadius = 16f;

        [Header("Direction Bias")]
        [Tooltip("Half-angle of the arc used when a spawn hint asks for a direction, at full hint weight. " +
                 "Small = a tight burst exactly where asked; large = a loose lean in that direction.")]
        [Range(5f, 180f)]
        public float SpreadAngle = 55f;

        protected override Vector3 SampleCandidate(Vector3 focus)
        {
            Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
            if (dir == Vector2.zero) dir = Vector2.up;
            return focus + new Vector3(dir.x, 0f, dir.y) * SampleRadius();
        }

        protected override Vector3 SampleCandidate(Vector3 focus, in SpawnHint hint)
        {
            if (!hint.HasDirection) return SampleCandidate(focus);

            // Widen the arc as the hint weakens: a weak hint barely leans, a strong one lands inside
            // SpreadAngle. At weight 0 the arc is the full circle, which is exactly the unbiased case.
            float weight = Mathf.Clamp01(hint.Weight);
            float halfArc = Mathf.Lerp(180f, SpreadAngle, weight);
            float angle = UnityEngine.Random.Range(-halfArc, halfArc);

            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * hint.Direction.normalized;
            return focus + direction * SampleRadius();
        }

        private float SampleRadius()
        {
            return UnityEngine.Random.Range(MinRadius, Mathf.Max(MinRadius, MaxRadius));
        }
    }
}
