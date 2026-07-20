using System.Collections.Generic;

namespace Kuantech.Core
{
    /// <summary>
    /// Tracks the actors this one is responsible for — drones, summons, totems, turrets — without
    /// parenting them in the hierarchy, so an owned actor can move freely in the world and still be
    /// cleaned up with its owner.
    ///
    /// This is a LIFECYCLE authority, not a lookup index. Whatever spawned the owned actor (a perk, a
    /// skill, an item) keeps its own reference to it; registering here only guarantees nothing is left
    /// orphaned in the world when the owner despawns or resets.
    ///
    /// Only register things that would otherwise outlive the owner. Short-lived, self-expiring effects
    /// (a burning patch, a projectile) already clean themselves up and do not belong here.
    /// </summary>
    public class OwnershipModule : ActorModule
    {
        private readonly List<Actor> _owned = new();

        public IReadOnlyList<Actor> Owned => _owned;

        /// <summary>
        /// Takes responsibility for an actor. Registering is idempotent, and the actor drops out of the
        /// list by itself if it despawns on its own (so the list never holds dead references).
        /// </summary>
        public void Register(Actor owned)
        {
            if (owned == null || owned == Actor || _owned.Contains(owned)) return;
            _owned.Add(owned);
            owned.OnDespawnedEvent += OnOwnedDespawned;
        }

        /// <summary>Stops tracking an actor without despawning it — it is on its own from here.</summary>
        public void Unregister(Actor owned)
        {
            if (owned == null) return;
            owned.OnDespawnedEvent -= OnOwnedDespawned;
            _owned.Remove(owned);
        }

        /// <summary>Stops tracking everything without despawning any of it.</summary>
        public void UnregisterAll()
        {
            foreach (var owned in _owned)
                if (owned != null) owned.OnDespawnedEvent -= OnOwnedDespawned;
            _owned.Clear();
        }

        /// <summary>
        /// Despawns everything this actor owns. This is the point of the module — called automatically on
        /// reset and cleanup, but also callable directly (e.g. a perk being removed).
        /// </summary>
        public void ReleaseAll()
        {
            // Backwards, and unsubscribe before despawning: Despawn fires OnDespawnedEvent, which would
            // otherwise re-enter OnOwnedDespawned and mutate the list we are iterating.
            for (int i = _owned.Count - 1; i >= 0; i--)
            {
                Actor owned = _owned[i];
                if (owned == null) continue;
                owned.OnDespawnedEvent -= OnOwnedDespawned;
                owned.Despawn();
            }
            _owned.Clear();
        }

        // An owned actor died/despawned on its own terms — just stop tracking it.
        private void OnOwnedDespawned(Actor owned)
        {
            if (owned == null) return;
            owned.OnDespawnedEvent -= OnOwnedDespawned;
            _owned.Remove(owned);
        }

        // The owner is being reused (respawn, run restart) — nothing it owned should survive that.
        public override void ResetModule()
        {
            base.ResetModule();
            ReleaseAll();
        }

        public override void Cleanup()
        {
            base.Cleanup();
            ReleaseAll();
        }
    }
}
