using System;
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
                 "obstacle check + retry loop rejects any that are currently blocked.")]
        public Transform[] SpawnPoints;

        protected override Vector3 SampleCandidate(Vector3 focus)
        {
            if (SpawnPoints == null || SpawnPoints.Length == 0) return focus;
            Transform point = SpawnPoints[UnityEngine.Random.Range(0, SpawnPoints.Length)];
            return point != null ? point.position : focus;
        }
    }
}
