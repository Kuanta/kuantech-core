using System.Collections.Generic;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.HordeBonkers
{
    /// <summary>
    /// Physics-free anti-overlap for large crowds. Agents register themselves (optionally with their own
    /// personal-space radius), and each frame the grid buckets them into a uniform spatial hash. Querying
    /// <see cref="GetSeparation"/> returns a push-away vector computed only from nearby agents — O(local
    /// density) per query, no colliders, no rigidbodies.
    ///
    /// Two agents' radii add together to decide how far apart they push: a bulky agent (bigger radius)
    /// keeps everyone farther away and shoves harder than a small one, instead of every agent in the game
    /// separating at the same fixed distance regardless of size.
    ///
    /// This is a steering input: callers fold the result into their movement vector, they do not
    /// move the transform here. Pairs naturally with a future flow field (flow = global direction,
    /// separation = local spacing).
    /// </summary>
    public class CrowdSeparationGrid : MonoBehaviour
    {
        public static CrowdSeparationGrid Instance { get; private set; }

        [Tooltip("Personal-space radius used for an agent that registers without its own (and the basis for " +
                 "the spatial hash's cell size). Two default-radius agents separate at 1m combined, matching " +
                 "this system's previous fixed 1m behaviour.")]
        public float DefaultAgentRadius = 0.5f;

        private readonly HashSet<Actor> _agents = new HashSet<Actor>();
        private readonly Dictionary<Actor, float> _agentRadii = new Dictionary<Actor, float>();
        private readonly Dictionary<long, List<Actor>> _buckets = new Dictionary<long, List<Actor>>();

        // Largest radius seen among registered agents. Grows as bulkier agents register so GetSeparation
        // knows how many neighbour cells it must search to never miss a valid push; only grows (never
        // shrinks back down when the biggest agent leaves) — a slightly wider search than strictly needed
        // is cheap, a missed push on a big enemy is a visible bug.
        private float _maxAgentRadius;

        private float CellSize => Mathf.Max(0.1f, DefaultAgentRadius * 2f);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            _maxAgentRadius = DefaultAgentRadius;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Registers <paramref name="agent"/> with its own personal-space radius (defaults to
        /// <see cref="DefaultAgentRadius"/> when not given). Bigger radius = keeps others farther away and
        /// pushes harder when crowded.
        /// </summary>
        public void Register(Actor agent, float radius = -1f)
        {
            if (agent == null) return;
            float r = radius > 0f ? radius : DefaultAgentRadius;
            _agents.Add(agent);
            _agentRadii[agent] = r;
            if (r > _maxAgentRadius) _maxAgentRadius = r;
        }

        public void Unregister(Actor agent)
        {
            if (agent == null) return;
            _agents.Remove(agent);
            _agentRadii.Remove(agent);
        }

        private float GetAgentRadius(Actor agent)
        {
            return _agentRadii.TryGetValue(agent, out float r) ? r : DefaultAgentRadius;
        }

        // Rebuilt after everyone has moved; queries next frame read a one-frame-old grid (imperceptible).
        private void LateUpdate()
        {
            foreach (var kv in _buckets) kv.Value.Clear();

            float cs = CellSize;
            foreach (var agent in _agents)
            {
                if (agent == null) continue;
                Vector3 p = agent.transform.position;
                long key = CellKey(Mathf.FloorToInt(p.x / cs), Mathf.FloorToInt(p.z / cs));
                if (!_buckets.TryGetValue(key, out var list))
                {
                    list = new List<Actor>();
                    _buckets[key] = list;
                }
                list.Add(agent);
            }
        }

        /// <summary>
        /// Returns a push-away vector for <paramref name="self"/> based on how crowded it is by
        /// nearby agents. Not normalized — its magnitude scales with crowding, so callers can weight it.
        /// Each neighbour is checked against <c>self radius + neighbour radius</c>, not a single shared
        /// distance, so a bulky agent keeps everyone farther away than a small one would.
        /// </summary>
        public Vector3 GetSeparation(Actor self)
        {
            if (self == null) return Vector3.zero;

            float cs = CellSize;
            float selfRadius = GetAgentRadius(self);
            // Search enough neighbour cells to cover the worst case: self at max personal-space distance
            // from the largest other agent registered. For ordinary same-sized agents this stays 1 (same
            // cost as before); it only widens around an oversized agent like an elite or boss.
            int range = Mathf.Max(1, Mathf.CeilToInt((selfRadius + _maxAgentRadius) / cs));

            Vector3 p = self.transform.position;
            int cx = Mathf.FloorToInt(p.x / cs);
            int cz = Mathf.FloorToInt(p.z / cs);

            Vector3 sum = Vector3.zero;
            for (int dx = -range; dx <= range; dx++)
            for (int dz = -range; dz <= range; dz++)
            {
                if (!_buckets.TryGetValue(CellKey(cx + dx, cz + dz), out var list)) continue;
                for (int i = 0; i < list.Count; i++)
                {
                    Actor other = list[i];
                    if (other == self || other == null) continue;

                    float personalSpace = selfRadius + GetAgentRadius(other);
                    Vector3 diff = p - other.transform.position;
                    diff.y = 0f;
                    float dSq = diff.sqrMagnitude;
                    if (dSq > personalSpace * personalSpace || dSq < 1e-6f) continue;

                    float d = Mathf.Sqrt(dSq);
                    sum += (diff / d) * (1f - d / personalSpace); // stronger the closer they are
                }
            }
            return sum;
        }

        /// <summary>
        /// Fills <paramref name="results"/> with every registered agent within <paramref name="radius"/>
        /// of <paramref name="center"/> (XZ distance). Non-allocating: the caller owns the list, which
        /// is cleared first. Lets target detection reuse this grid instead of physics overlap queries.
        /// </summary>
        public void QueryRadius(Vector3 center, float radius, List<Actor> results)
        {
            if (results == null) return;
            results.Clear();

            float cs = CellSize;
            float radiusSq = radius * radius;
            int cx = Mathf.FloorToInt(center.x / cs);
            int cz = Mathf.FloorToInt(center.z / cs);
            int range = Mathf.CeilToInt(radius / cs);

            for (int dx = -range; dx <= range; dx++)
            for (int dz = -range; dz <= range; dz++)
            {
                if (!_buckets.TryGetValue(CellKey(cx + dx, cz + dz), out var list)) continue;
                for (int i = 0; i < list.Count; i++)
                {
                    Actor other = list[i];
                    if (other == null) continue;
                    Vector3 diff = other.transform.position - center;
                    diff.y = 0f;
                    if (diff.sqrMagnitude <= radiusSq) results.Add(other);
                }
            }
        }

        /// <summary>
        /// Returns the closest registered agent within <paramref name="radius"/> of
        /// <paramref name="center"/>, or null if none. Allocation-free.
        /// </summary>
        public Actor GetNearest(Vector3 center, float radius)
        {
            float cs = CellSize;
            float radiusSq = radius * radius;
            int cx = Mathf.FloorToInt(center.x / cs);
            int cz = Mathf.FloorToInt(center.z / cs);
            int range = Mathf.CeilToInt(radius / cs);

            Actor nearest = null;
            float bestSq = radiusSq;
            for (int dx = -range; dx <= range; dx++)
            for (int dz = -range; dz <= range; dz++)
            {
                if (!_buckets.TryGetValue(CellKey(cx + dx, cz + dz), out var list)) continue;
                for (int i = 0; i < list.Count; i++)
                {
                    Actor other = list[i];
                    if (other == null) continue;
                    Vector3 diff = other.transform.position - center;
                    diff.y = 0f;
                    float dSq = diff.sqrMagnitude;
                    if (dSq <= bestSq)
                    {
                        bestSq = dSq;
                        nearest = other;
                    }
                }
            }
            return nearest;
        }

        private static long CellKey(int x, int z) => ((long)x << 32) ^ (uint)z;
    }
}
