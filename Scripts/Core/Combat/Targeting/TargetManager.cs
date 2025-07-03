using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core
{
    public class TargetManager : ActorModule
    {
        [Header("Target Detection")]
        [Header("Slot Allocator")]

        public TargetSlotAllocator SlotAllocator;
        public int SlotCount = 0;
        public float Radius;
        
        //Runtime
        public TargetSlot CurrentTargetSlot;
        public Actor CurrentTarget;

        public override void Initialize()
        {
            if (Initialized) return;
            base.Initialize();
            if (SlotCount == 0) SlotAllocator = null;
            else
            {
                SlotAllocator = new TargetSlotAllocator(Actor.transform,SlotCount, Radius, Actor.ActorForwardVector, Actor.ActorUpVector);
            }
        }
        public bool SetCurrentTarget(Actor target)
        {
            if (CurrentTarget != null && CurrentTarget == target) return true; //Don't change target 
            UnsetCurrentTarget();
            CurrentTarget = target;
            return true;
        }
        
        /// <summary>
        /// Tries to assign this actor to a 'melee slot' 
        /// </summary>
        public void AssignToTargetSlot(Actor otherActor)
        {
            UnsetCurrentTargetSlot();
            TargetManager otherManager = otherActor.GetModule<TargetManager>();
            TargetSlot slot = null;
            if (otherManager != null && otherManager.SlotAllocator != null)
            {
                slot = otherManager.SlotAllocator.GetBestSlot(Actor);
                if (slot == null) return;
                //Register to slot
                otherManager.SlotAllocator.RegisterActorToSlot(Actor, slot.Index);
            }

            CurrentTargetSlot = slot;
        }
        
        public Actor GetCurrentTarget()
        {
            return CurrentTarget;
        }

        public void UnsetCurrentTargetSlot()
        {
            if (CurrentTargetSlot != null)
            {
                //Clear slot
                CurrentTargetSlot.OccupyingActor = null;
            }

            CurrentTargetSlot = null;
        }
        
        public void UnsetCurrentTarget()
        {
            CurrentTarget = null;
            UnsetCurrentTargetSlot();
        }

        public WorldPoint GetTargetPoint()
        {
            if (CurrentTargetSlot != null)
            {
                return CurrentTargetSlot.WorldPoint;
            }
            
            return new WorldPoint()
            {
                Target = CurrentTarget.GetHitPoint(),
            };
        }
    }
}