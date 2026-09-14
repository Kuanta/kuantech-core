using Kuantech.Core;
using UnityEngine;

namespace Kuantech.HordeBonkers
{
    /// <summary>
    /// Shared steering for a dumb horde agent (grunt or worker): turns a desired direction into an actual
    /// move vector by folding in crowd separation (anti-overlap) and local obstacle avoidance (whisker
    /// rays), then drives the shared <see cref="MovementModule"/>. Passive — it acts only when a brain
    /// calls <see cref="Move"/>, so it never fights a brain that also writes the vector. Deliberately NOT
    /// NavMesh — hundreds of agents can't each carry an agent; bosses (few, NavMesh + BT) bring their own
    /// locomotion. The brain decides WHERE to go; this module decides HOW to get there.
    /// </summary>
    public class SteeringMovementModule : ActorModule
    {
        [Tooltip("How strongly this agent pushes away from crowding neighbours.")]
        [SerializeField] private float SeparationWeight = 1f;

        [Header("Obstacle Avoidance")]
        [Tooltip("Layers treated as obstacles to steer around (walls, destructibles). Unset = no avoidance.")]
        [SerializeField] private LayerMask ObstacleMask;
        [Tooltip("How far ahead the feeler rays probe for obstacles.")]
        [SerializeField] private float ProbeDistance = 1.5f;
        [Tooltip("Angle of the two side feelers from the forward probe (degrees).")]
        [SerializeField] private float FeelerAngle = 30f;
        [Tooltip("How hard a detected obstacle pushes the steer away from it.")]
        [SerializeField] private float AvoidanceWeight = 2f;
        [Tooltip("How much avoidance slides ALONG the wall (toward the goal) vs. pushing straight back off " +
                 "it. Higher = flows around obstacles instead of stalling head-on against them.")]
        [SerializeField] private float SlideWeight = 1.5f;
        [Tooltip("Height above the actor's feet to cast the feelers from, so they hit walls not the floor.")]
        [SerializeField] private float ProbeHeight = 0.5f;
        [Tooltip("Seconds between obstacle probes — throttled so a big horde stays cheap (result is reused between probes).")]
        [SerializeField] private float ProbeInterval = 0.1f;

        [SerializeField] private bool EnableObstacleAvoidance = true;
        [SerializeField] private bool EnableCrowdSeperation = true;

        private MovementModule _movementModule;
        private Vector3 _cachedAvoidance;
        private float _lastProbeTime;

        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            _movementModule = Actor.GetModule<MovementModule>();
        }

        // Register/unregister with the crowd grid so this agent participates in anti-overlap while alive.
        public override void OnActorStateChanged(ActorState oldState, ActorState newState)
        {
            base.OnActorStateChanged(oldState, newState);
            if (newState == ActorState.Spawned)
                // Actor.ActorRadius is the same "how big am I" the rest of the game already uses (attack
                // range, surround-slot count) — reuse it here instead of a separate crowd-only size.
                CrowdSeparationGrid.Instance?.Register(Actor, Actor.ActorRadius);
            else if (newState == ActorState.Dead || newState == ActorState.Despawned)
                CrowdSeparationGrid.Instance?.Unregister(Actor);
        }

        /// <summary>
        /// Steers toward <paramref name="seekDir"/> (may be zero to hold position while still spreading),
        /// folding in crowd separation and obstacle avoidance, and writes the result to the mover.
        /// </summary>
        public void Move(Vector3 seekDir)
        {
            if (_movementModule == null) return;
            seekDir.y = 0f;

            Vector3 separation = EnableCrowdSeperation && CrowdSeparationGrid.Instance != null
                ? CrowdSeparationGrid.Instance.GetSeparation(Actor)
                : Vector3.zero;

            // Avoidance steers off the intended travel; if we're only separating (no seek), avoid along that.
            Vector3 travel = seekDir.sqrMagnitude > 1e-6f ? seekDir : separation;
            Vector3 avoidance = EnableObstacleAvoidance ? GetObstacleAvoidance(travel) : Vector3.zero;

            Vector3 move = seekDir + SeparationWeight * separation + AvoidanceWeight * avoidance;
            move.y = 0f;
            if (move.sqrMagnitude > 1f) move.Normalize();
            _movementModule.SetMovementVector(move);
        }

        public void Stop()
        {
            if (_movementModule != null) _movementModule.Stop();
        }

        // Fans three short feelers (forward + two angled) along the travel direction; each obstacle hit adds
        // a push along the surface normal. Throttled: the last result is reused between probes so the
        // per-frame cost across a big horde stays low.
        private Vector3 GetObstacleAvoidance(Vector3 travelDir)
        {
            if (ObstacleMask == 0 || travelDir.sqrMagnitude < 1e-6f) return Vector3.zero;

            if (Time.time - _lastProbeTime < ProbeInterval) return _cachedAvoidance;
            _lastProbeTime = Time.time - Random.Range(0f, ProbeInterval);

            Vector3 forward = travelDir.normalized;
            Vector3 origin = Actor.GetActorLocation();
            origin.y += ProbeHeight;

            Vector3 avoid = Vector3.zero;
            avoid += ProbeFeeler(origin, forward, forward);
            avoid += ProbeFeeler(origin, Quaternion.AngleAxis(FeelerAngle, Vector3.up) * forward, forward);
            avoid += ProbeFeeler(origin, Quaternion.AngleAxis(-FeelerAngle, Vector3.up) * forward, forward);

            _cachedAvoidance = avoid;
            return avoid;
        }

        // One feeler: on an obstacle hit, return a push that both backs off the wall (along its normal) AND
        // slides along it toward where we're headed. The slide component is what stops a head-on standoff —
        // without it, an agent whose goal is past the wall just presses into it (push-in vs push-out cancel).
        private Vector3 ProbeFeeler(Vector3 origin, Vector3 dir, Vector3 travelDir)
        {
            if (!UnityEngine.Physics.Raycast(origin, dir, out RaycastHit hit, ProbeDistance, ObstacleMask))
                return Vector3.zero;

            Vector3 normal = hit.normal;
            normal.y = 0f;
            if (normal.sqrMagnitude < 1e-6f) return Vector3.zero;
            normal.Normalize();

            // Wall tangent (in the ground plane), flipped to point the way we want to travel — so we slide
            // around the obstacle rather than orbit back the way we came.
            Vector3 tangent = Vector3.Cross(Vector3.up, normal);
            if (Vector3.Dot(tangent, travelDir) < 0f) tangent = -tangent;

            float closeness = 1f - Mathf.Clamp01(hit.distance / ProbeDistance);
            return (normal + tangent * SlideWeight) * closeness;
        }
    }
}
