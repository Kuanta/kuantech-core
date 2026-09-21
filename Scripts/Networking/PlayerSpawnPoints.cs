using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Networking
{
    /// <summary>
    /// Marks where players enter a level. It registers itself rather than being wired up by whoever
    /// spawns players, because the level is not necessarily part of the scene -- the arena, for one, is
    /// instantiated from a prefab after the scene is already up, so a spawner that went looking for
    /// spawn points at scene load would find nothing.
    /// </summary>
    public class PlayerSpawnPoints : MonoBehaviour
    {
        [Tooltip("Players are placed one per point, in order, wrapping around when there are more " +
                 "players than points.")]
        [SerializeField] private Transform[] Points;

        private static readonly List<PlayerSpawnPoints> Registered = new List<PlayerSpawnPoints>();

        private void OnEnable()
        {
            if (!Registered.Contains(this)) Registered.Add(this);
        }

        private void OnDisable()
        {
            Registered.Remove(this);
        }

        /// <summary>
        /// Whether any level geometry has registered somewhere to stand yet. A spawner that runs off a
        /// connection callback can easily beat the level into existence, and this is how it waits.
        /// </summary>
        public static bool HasAny
        {
            get
            {
                foreach (PlayerSpawnPoints set in Registered)
                {
                    if (set != null && set.Points != null && set.Points.Length > 0) return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Resolves the spawn pose for the given player index. Falls back to the world origin when the
        /// level has no spawn points registered, so a missing set costs a warning, not a broken match.
        /// </summary>
        public static void GetSpawn(int playerIndex, out Vector3 position, out Quaternion rotation)
        {
            foreach (PlayerSpawnPoints set in Registered)
            {
                if (set == null || set.Points == null || set.Points.Length == 0) continue;

                Transform point = set.Points[playerIndex % set.Points.Length];
                if (point == null) continue;

                position = point.position;
                rotation = point.rotation;
                return;
            }

            Debug.LogWarning($"[PlayerSpawnPoints] No spawn points registered -- player {playerIndex} " +
                             "will spawn at the world origin. Add a PlayerSpawnPoints to the level.");
            position = Vector3.zero;
            rotation = Quaternion.identity;
        }
    }
}
