using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.HordeSurvival
{
    /// <summary>
    /// Spawns at one of a fixed set of predefined points (spawn gates, doorways, ...) instead of an
    /// annulus around a moving focus. `focus`/`hint` are ignored -- every point is equally eligible, the
    /// base class's obstacle-rejection retry loop just picks among them until one comes up clear.
    /// </summary>
    [Serializable]
    public class FixedPointSpawnScheme : SpawnScheme
    {
        [Tooltip("Candidate spawn points. One is picked at random per candidate sample; the base class's " +
                 "obstacle check + retry loop rejects any that are currently blocked. A List rather than an " +
                 "array so newly-opened rooms can append their own spawners at runtime (see AddSpawnPoints).")]
        public List<Transform> SpawnPoints = new List<Transform>();

        protected override Vector3 SampleCandidate(Vector3 focus)
        {
            if (SpawnPoints == null || SpawnPoints.Count == 0) return focus;
            Transform point = SpawnPoints[UnityEngine.Random.Range(0, SpawnPoints.Count)];
            return point != null ? point.position : focus;
        }

        /// <summary>Merges in points from a newly-opened room. Skips nulls and points already present.</summary>
        public void AddSpawnPoints(IEnumerable<Transform> points)
        {
            if (points == null) return;
            SpawnPoints ??= new List<Transform>();
            foreach (Transform point in points)
            {
                if (point != null && !SpawnPoints.Contains(point))
                    SpawnPoints.Add(point);
            }
        }

        /// <summary>Drops every point merged in at runtime by opened rooms. Called on a run reset so a
        /// second run starts from empty and re-accumulates only whichever rooms are open again -- without
        /// this, a room opened in a previous run would keep spawning zombies forever, door or no door.</summary>
        public void ClearSpawnPoints() => SpawnPoints?.Clear();
    }
}
