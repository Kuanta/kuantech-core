using System;
using System.Collections.Generic;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.HordeSurvival
{
    /// <summary>
    /// Decides *which way* the horde should arrive from, so the player keeps feeling surrounded instead of
    /// towing a blob behind them.
    ///
    /// Two pressures are balanced. Enemies should come from where the player is heading — otherwise running
    /// away buys permanent safety — and from wherever the ring is currently thin, so the horde spreads back
    /// out instead of piling into one clump. Both are read off the same cheap structure: the circle around
    /// the player is cut into sectors and each living enemy is dropped into one. That is a single O(enemies)
    /// pass with no allocation and no physics, and it runs at most once per frame no matter how many spawns
    /// or relocations ask for a direction in that frame.
    /// </summary>
    [Serializable]
    public class HordeSpawnDirector
    {
        [Tooltip("How many angular sectors the circle around the player is split into. More sectors = finer " +
                 "reading of where the gaps are, at a proportionally tiny cost.")]
        [Range(4, 32)]
        public int SectorCount = 12;

        [Tooltip("Enemies farther than this from the player are ignored when measuring crowding — they are " +
                 "already out of the fight and should not make their sector look busy.")]
        public float OccupancyRadius = 25f;

        [Tooltip("How strongly spawns are pulled toward the player's direction of travel. This is what stops " +
                 "running away from working: flee one way and the horde starts arriving from that way.")]
        [Range(0f, 1f)]
        public float TravelWeight = 0.6f;

        [Tooltip("How strongly spawns are pulled toward the emptiest sectors, re-spreading a clumped horde.")]
        [Range(0f, 1f)]
        public float GapWeight = 0.4f;

        [Tooltip("Passed to the spawn scheme as the hint strength. Lower it to loosen the bias if arrivals " +
                 "start to look too obviously scripted.")]
        [Range(0f, 1f)]
        public float HintWeight = 0.75f;

        [Tooltip("Sectors scoring within this much of the best one are treated as equally good and picked " +
                 "between at random. Without it every enemy in a batch would target one identical sector — " +
                 "which is just a new blob in a new place.")]
        [Range(0f, 1f)]
        public float TieTolerance = 0.15f;

        // Occupancy per sector, rebuilt at most once per frame. Sized lazily so SectorCount stays editable.
        private float[] _occupancy;
        private float[] _scores;
        private int _lastRefreshFrame = -1;

        /// <summary>
        /// Produces the direction the next arrival should come from. <paramref name="travelDirection"/> is
        /// the player's recent movement on the XZ plane (zero when standing still, in which case only the
        /// gap pressure applies and the horde simply re-spreads).
        /// </summary>
        public SpawnHint GetHint(Vector3 focus, Vector3 travelDirection, IReadOnlyCollection<Actor> enemies)
        {
            EnsureBuffers();
            Refresh(focus, enemies);

            float sectorArc = 360f / SectorCount;
            bool hasTravel = travelDirection.sqrMagnitude > 0.0001f;
            Vector3 travel = hasTravel ? travelDirection.normalized : Vector3.zero;

            // Normalise crowding against the busiest sector, so the gap term means "empty relative to the
            // rest of the ring" rather than depending on how many enemies happen to be alive.
            float busiest = 0f;
            for (int i = 0; i < SectorCount; i++)
                if (_occupancy[i] > busiest) busiest = _occupancy[i];

            float best = float.NegativeInfinity;
            for (int i = 0; i < SectorCount; i++)
            {
                Vector3 sectorDirection = SectorDirection(i, sectorArc);

                // Dot is -1..1 behind..ahead; remapped to 0..1 so it can be weighed against the gap term.
                float travelScore = hasTravel ? (Vector3.Dot(sectorDirection, travel) + 1f) * 0.5f : 0.5f;
                float gapScore = busiest > 0f ? 1f - (_occupancy[i] / busiest) : 1f;

                float score = TravelWeight * travelScore + GapWeight * gapScore;
                _scores[i] = score;
                if (score > best) best = score;
            }

            // Pick at random among the sectors that tied for best, so a batch fans out across the good arc.
            int candidates = 0;
            for (int i = 0; i < SectorCount; i++)
                if (_scores[i] >= best - TieTolerance) candidates++;

            int chosen = UnityEngine.Random.Range(0, Mathf.Max(1, candidates));
            for (int i = 0; i < SectorCount; i++)
            {
                if (_scores[i] < best - TieTolerance) continue;
                if (chosen-- > 0) continue;
                return new SpawnHint { Direction = SectorDirection(i, sectorArc), Weight = HintWeight };
            }

            return SpawnHint.None;
        }

        /// <summary>Direction through the middle of a sector.</summary>
        private Vector3 SectorDirection(int index, float sectorArc)
        {
            float angle = (index + 0.5f) * sectorArc * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
        }

        /// <summary>
        /// Bins living enemies into sectors. Guarded by frame number because a spawn batch calls GetHint
        /// several times in a row and the answer cannot meaningfully change between those calls.
        /// </summary>
        private void Refresh(Vector3 focus, IReadOnlyCollection<Actor> enemies)
        {
            if (_lastRefreshFrame == Time.frameCount) return;
            _lastRefreshFrame = Time.frameCount;

            Array.Clear(_occupancy, 0, _occupancy.Length);
            if (enemies == null) return;

            float sqrRadius = OccupancyRadius * OccupancyRadius;
            float sectorArc = 360f / SectorCount;

            foreach (Actor enemy in enemies)
            {
                if (enemy == null) continue;

                Vector3 offset = enemy.GetActorLocation() - focus;
                offset.y = 0f;
                float sqrDistance = offset.sqrMagnitude;
                if (sqrDistance < 0.0001f || sqrDistance > sqrRadius) continue;

                float angle = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
                if (angle < 0f) angle += 360f;

                int sector = Mathf.Clamp((int)(angle / sectorArc), 0, SectorCount - 1);
                _occupancy[sector] += 1f;
            }
        }

        private void EnsureBuffers()
        {
            SectorCount = Mathf.Clamp(SectorCount, 4, 32);
            if (_occupancy == null || _occupancy.Length != SectorCount)
            {
                _occupancy = new float[SectorCount];
                _scores = new float[SectorCount];
                _lastRefreshFrame = -1;
            }
        }
    }
}
