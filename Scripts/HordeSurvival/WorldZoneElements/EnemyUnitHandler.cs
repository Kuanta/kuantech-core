using System.Collections.Generic;
using Kuantech.Core;

namespace Kuantech.HordeSurvival
{
    /// <summary>
    /// Tracks every enemy actor currently in the arena — alive and (ragdoll) dead-but-not-despawned —
    /// decoupled from wave/budget concerns. Anything that just wants "who's out there" (turret AI,
    /// auto-cast targeting, the player's yeet-aim sort) looks this up directly instead of reaching
    /// through HordeWaveHandler. HordeWaveHandler still owns spawning (budget, roster, placement) and
    /// hands every enemy it creates to <see cref="RegisterEnemy"/>; other spawners (test tooling,
    /// wave events) can register directly too, as long as they're both children of the same Arena.
    /// </summary>
    public class EnemyUnitHandler : WorldZoneElement
    {
        // All enemies present in the world (alive + ragdoll corpses not yet despawned). Removed on despawn.
        private readonly HashSet<Actor> _spawnedEnemies = new HashSet<Actor>();
        // Only alive enemies (the actual threats). Removed on death.
        private readonly HashSet<Actor> _aliveEnemies = new HashSet<Actor>();

        public IReadOnlyCollection<Actor> SpawnedEnemies => _spawnedEnemies;
        public IReadOnlyCollection<Actor> AliveEnemies => _aliveEnemies;
        public int AliveCount => _aliveEnemies.Count;
        public int SpawnedCount => _spawnedEnemies.Count;

        /// <summary>
        /// Starts tracking an enemy actor already spawned into the world. Idempotent (-=/+=) so a pooled
        /// actor reused without a clean despawn — its despawn deferred to a coroutine a scene change
        /// killed — doesn't end up with its death/despawn handlers subscribed twice.
        /// </summary>
        public void RegisterEnemy(Actor actor)
        {
            if (actor == null) return;
            _spawnedEnemies.Add(actor);
            _aliveEnemies.Add(actor);
            actor.OnDeathEvent -= OnEnemyDeath;
            actor.OnDeathEvent += OnEnemyDeath;
            actor.OnDespawnedEvent -= OnEnemyDespawned;
            actor.OnDespawnedEvent += OnEnemyDespawned;
        }

        /// <summary>
        /// Enemy died: remove from the alive set but keep it in the present set — the corpse is
        /// still flying around as a ragdoll.
        /// </summary>
        private void OnEnemyDeath(Actor actor)
        {
            if (actor == null) return;
            actor.OnDeathEvent -= OnEnemyDeath;
            _aliveEnemies.Remove(actor);
        }

        /// <summary>
        /// Enemy despawned (corpse gone too): remove from both sets. If it despawned without dying
        /// (e.g. ClearAllEnemies), it also drops from the alive set.
        /// </summary>
        private void OnEnemyDespawned(Actor actor)
        {
            if (actor == null) return;
            actor.OnDeathEvent -= OnEnemyDeath;
            actor.OnDespawnedEvent -= OnEnemyDespawned;
            _aliveEnemies.Remove(actor);
            _spawnedEnemies.Remove(actor);
        }

        /// <summary>Despawns every tracked enemy and clears both sets (wave reset / level cleanup).</summary>
        public void ClearAllEnemies()
        {
            // OnEnemyDespawned mutates the sets, so iterate over a copy.
            var snapshot = new List<Actor>(_spawnedEnemies);
            foreach (var actor in snapshot)
            {
                if (actor != null) actor.Despawn(0f);
            }
            _spawnedEnemies.Clear();
            _aliveEnemies.Clear();
        }

        public override void CleanupZone()
        {
            base.CleanupZone();
            ClearAllEnemies();
        }
    }
}
