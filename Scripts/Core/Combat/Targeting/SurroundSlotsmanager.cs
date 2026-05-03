using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core
{
    /// <summary>
    /// Manages the surround slots around this actor.
    /// Attach to any actor that can be surrounded (player, boss, etc.).
    /// Enemies that want to surround this actor use SurroundHandler to claim a slot.
    ///
    /// Effective radius = Actor.ActorRadius + RadiusOffset.
    /// This way large actors (big boss) naturally get more surrounding slots,
    /// and RadiusOffset lets you push slots just outside the collider surface.
    /// Attacker melee range should be >= effective radius to guarantee attacks land.
    /// </summary>
    public class SurroundSlotsManager : ActorModule
    {
        [Header("Slots")]
        [Tooltip("Added on top of Actor.ActorRadius. Keep >= attacker collider radius so enemies don't overlap the target.")]
        public float RadiusOffset = 0.5f;

        [Tooltip("Arc length between adjacent slots. Set to roughly attacker shoulder width.")]
        public float SurroundSlotsDistance = 0.8f;

        [Header("Debug")]
        [SerializeField] private bool DebugSlots;
        [SerializeField] private GameObject SlotDebugIndicator;

        public TargetSlotAllocator SlotAllocator { get; private set; }

        // Actors currently targeting this actor (informational — lets AI check how contested this target is)
        public HashSet<Actor> TargetedByActors = new();

        /// <summary>Returns the radius at which slots are placed.</summary>
        public float GetEffectiveRadius() => Mathf.Max(0.1f, Actor.ActorRadius + RadiusOffset);

        public override void OnActorStateChanged(ActorState oldState, ActorState newState)
        {
            if (newState == ActorState.Spawned)
                RebuildSlots();
            else if (newState == ActorState.Despawned || newState == ActorState.Dead)
                ReleaseAllSlots();
        }

        public void RebuildSlots()
        {
            ClearDebugVisuals();
            int count = GetSlotCount();
            if (count == 0) { SlotAllocator = null; return; }
            SlotAllocator = new TargetSlotAllocator(Actor, count, GetEffectiveRadius(),
                Actor.ActorForwardVector, Actor.ActorUpVector)
            {
                DebugVisualizationObjectPrefab = SlotDebugIndicator
            };
        }

        // Frees all slots without destroying the allocator (e.g. on death so attackers can re-queue)
        private void ReleaseAllSlots()
        {
            if (SlotAllocator?.Slots == null) return;
            foreach (var slot in SlotAllocator.Slots)
                slot.OccupyingActor = null;
        }

        public int GetSlotCount()
        {
            return Mathf.CeilToInt(2 * Mathf.PI * GetEffectiveRadius() / Mathf.Max(0.1f, SurroundSlotsDistance));
        }

        /// <summary>Dynamically resizes the slot ring, e.g. when a boss grows.</summary>
        public void SetRadiusOffset(float offset)
        {
            RadiusOffset = offset;
            RebuildSlots();
        }

        // Called by SurroundHandler when an attacker starts targeting this actor
        public void OnTargetedBy(Actor targeter)
        {
            TargetedByActors.Add(targeter);
        }

        // Called by SurroundHandler when an attacker stops targeting this actor
        public void OnUntargetedBy(Actor targeter)
        {
            TargetedByActors.Remove(targeter);
        }

        public override void ModuleUpdate()
        {
            base.ModuleUpdate();
            if (DebugSlots && Actor.IsAlive() && SlotAllocator != null)
                SlotAllocator.VisualizeSlots();
        }

        public override void ResetModule()
        {
            base.ResetModule();
            ReleaseAllSlots();
            TargetedByActors.Clear();
            ClearDebugVisuals();
        }

        public override void Cleanup()
        {
            base.Cleanup();
            ClearDebugVisuals();
        }

        private void ClearDebugVisuals()
        {
            if (SlotAllocator?.Slots.IsNullOrEmpty() != false) return;
            foreach (var slot in SlotAllocator.Slots)
            {
                if (slot.DebugVisualizationObject == null) continue;
                Helpers.DestroyGameObject(slot.DebugVisualizationObject);
                slot.DebugVisualizationObject = null;
            }
        }
    }
}
