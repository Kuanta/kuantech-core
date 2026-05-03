using Kuantech.Utils;

namespace Kuantech.Core
{
    /// <summary>
    /// Handles surrounding a target actor by claiming a slot from its SurroundSlotsManager.
    /// Attach to any actor that actively pursues and surrounds another actor (enemies, allies, etc.).
    /// The target must have a SurroundSlotsManager for slot-based positioning to work;
    /// if it doesn't, GetTargetPoint falls back to the target's hit point.
    /// </summary>
    public class SurroundHandler : ActorModule
    {
        public Actor CurrentTarget { get; private set; }
        public TargetSlot CurrentTargetSlot { get; private set; }

        /// <summary>
        /// Sets the target and notifies its SurroundSlotsManager. Does NOT claim a slot yet;
        /// call AssignToTargetSlot separately when ready to move into position.
        /// </summary>
        public void SetCurrentTarget(Actor target)
        {
            ClearTarget();
            CurrentTarget = target;
            if (target == null) return;
            Actor.MotionVectorsHandler.SetTargetObject(target.transform);
            var slotsManager = target.GetModule<SurroundSlotsManager>();
            if (slotsManager != null) slotsManager.OnTargetedBy(Actor);
        }

        /// <summary>
        /// Releases the current slot and target. Safe to call when already cleared.
        /// </summary>
        public void ClearTarget()
        {
            UnsetCurrentTargetSlot();
            if (CurrentTarget != null)
            {
                var slotsManager = CurrentTarget.GetModule<SurroundSlotsManager>();
                if (slotsManager != null) slotsManager.OnUntargetedBy(Actor);
            }
            CurrentTarget = null;
            Actor.MotionVectorsHandler.SetTargetObject(null);
        }

        /// <summary>
        /// Claims the best available slot on the target's SurroundSlotsManager.
        /// Call this after SetCurrentTarget when the actor is ready to move into melee position.
        /// </summary>
        public void AssignToTargetSlot(Actor targetActor,
            TargetDetectionSlotType detectionSlotType = TargetDetectionSlotType.ByDistance,
            bool inverted = false)
        {
            UnsetCurrentTargetSlot();
            var slotsManager = targetActor.GetModule<SurroundSlotsManager>();
            if (slotsManager == null || slotsManager.SlotAllocator == null) return;

            TargetSlot slot = slotsManager.SlotAllocator.GetBestSlot(Actor, detectionSlotType, inverted);
            if (slot == null) return;

            slotsManager.SlotAllocator.RegisterActorToSlot(Actor, slot.Index);
            CurrentTargetSlot = slot;
        }

        /// <summary>
        /// Releases the currently held slot without clearing the target.
        /// Useful when reassigning to a better slot mid-pursuit.
        /// </summary>
        public void UnsetCurrentTargetSlot()
        {
            if (CurrentTargetSlot != null && CurrentTargetSlot.OccupyingActor == Actor)
                CurrentTargetSlot.OccupyingActor = null;
            CurrentTargetSlot = null;
        }

        /// <summary>
        /// Returns the world point this actor should move toward.
        /// Returns the claimed slot position if one is assigned, otherwise the target's hit point.
        /// </summary>
        public WorldPoint GetTargetPoint()
        {
            if (CurrentTargetSlot != null) return CurrentTargetSlot.GetWorldPoint();
            if (CurrentTarget != null)     return CurrentTarget.GetHitPoint(Actor);
            return default;
        }

        public bool HasTarget() => CurrentTarget != null;
        public bool HasSlot()   => CurrentTargetSlot != null;

        public override void OnActorStateChanged(ActorState oldState, ActorState newState)
        {
            if (newState == ActorState.Dead || newState == ActorState.Despawned)
                ClearTarget();
        }

        public override void ResetModule()
        {
            base.ResetModule();
            ClearTarget();
        }
    }
}
