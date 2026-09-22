using System;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.HordeSurvival
{
    /// <summary>
    /// A horde arena. It is just a WorldZone: its elements (wave handler, worker handler, ...) are
    /// discovered via GetComponentsInChildren and driven through the generic zone lifecycle, so the
    /// arena never hard-references any specific handler.
    /// </summary>
    public class Arena : WorldZone
    {
        [Tooltip("Id this arena prefab is registered under on the ArenaManager. ArenaData.ArenaPrefabId " +
                 "points at this, and that is how a run resolves which arena to instantiate.")]
        public string ArenaId;

        public Transform PlayerSpawnPoint;
        [NonSerialized] public Actor Player;
        [NonSerialized] public HordeWaveHandler WaveHandler;
        [NonSerialized] public EnemyUnitHandler UnitHandler;

        /// <summary>
        /// Wires this arena instance for a run. EnemyBlueprints/SpawnScheme/SpawnDirector stay whatever the
        /// prefab authors them as (shared across every arena, not data-driven) -- only PowerLevel, the
        /// difficulty numbers and the wave-event script come from arenaData.
        /// </summary>
        public virtual void InitializeArena(Actor player, ArenaData arenaData)
        {
            Player = player;
            player.WarpToPoint(PlayerSpawnPoint.position, PlayerSpawnPoint.rotation);
            // Nothing game-specific is pushed onto the player here -- an arena knows nothing about a given
            // game's player modules. Whoever owns the run wires those from HordeSurvivalRunHandler's
            // OnPlayerSpawned instead.
            Initialize(); // WorldZone: detect child WorldZoneElements and Initialize(this) each

            // Feed the run's data to the wave handler before the zone activates (it reads these when it
            // builds wave 0). Set here so the arena, not each handler, owns run-level wiring.
            WaveHandler = GetZoneElementByType<HordeWaveHandler>();
            if (WaveHandler != null && arenaData != null)
            {
                WaveHandler.PowerLevel = Mathf.Max(1, arenaData.PowerLevel);

                // A fresh, run-owned DifficultyConfig -- every arena still shares the same Arena.prefab, so
                // mutating WaveHandler's originally-assigned (shared) asset would leak one arena's numbers
                // into every other one.
                DifficultyConfig difficulty = ScriptableObject.CreateInstance<DifficultyConfig>();
                difficulty.ApplyFromArenaData(arenaData);
                WaveHandler.Difficulty = difficulty;

                WaveHandler.WaveEvents = arenaData.WaveEvents ?? new System.Collections.Generic.List<WaveEvent>();
            }
            UnitHandler = GetZoneElementByType<EnemyUnitHandler>();
        }

        public void StartArena()
        {
            ActivateZone(); // WorldZone: OnZoneActivated on every element
        }

        public Vector3 GetPlayerPosition()
        {
            return Player != null ? Player.transform.position : transform.position;
        }

        public void ResetArena()
        {
            ResetZone();
        }
    }
}
