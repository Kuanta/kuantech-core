using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core
{
    public class SurroundManager : ActorModule
    {
        [Header("Slot Allocator")]
        public TargetSlotAllocator SlotAllocator;
        
        [Tooltip("Final slot is calculated by 2*PI*Radius / SurroundSlotsDistance")] 
        public float SurroundSlotsDistance;


        //Runtime
        public TargetSlot CurrentTargetSlot;
        
        //todo: Create a faction based target system
        private Actor CurrentTarget = null;

        public override void Initialize()
        {
            base.Initialize();
            Actor.OnActorRadiusSet += OnActorRadiusSet;
        }
        public override void OnActorStateChanged(ActorState oldState, ActorState newState)
        {
            if (newState == ActorState.Spawned)
            {
                SetSlotAllocator();
            }
        }

        private void SetSlotAllocator()
        {
            int slotCount = GetSlotCount();
            if (slotCount== 0) SlotAllocator = null;
            else
            {
                SlotAllocator = new TargetSlotAllocator(Actor, slotCount, Actor.ActorRadius, Actor.ActorForwardVector, Actor.ActorUpVector);
            }
        }

        public int GetSlotCount()
        {
            float radius = Mathf.Max(0.1f, Actor.ActorRadius);
            return Mathf.CeilToInt((2 * Mathf.PI * radius) / Mathf.Max(0.1f, SurroundSlotsDistance));
        }
        
        public void OnActorRadiusSet(object sender, float radius)
        {
            //Recalculate slot allocator
            SetSlotAllocator();
        }
        
        public bool SetCurrentTarget(Actor target)
        {
            if (CurrentTarget != null && CurrentTarget == target) return true; //Don't change target 
            Actor.MotionVectorsHandler.SetTargetObject(target.transform);
            ClearTarget();
            CurrentTarget = target;
            return true;
        }

        public void ClearTarget()
        {
            CurrentTarget = null;
            Actor.MotionVectorsHandler.SetTargetObject(null);
        }
        
        /// <summary>
        /// Tries to assign this actor to a 'melee slot' 
        /// </summary>
        public void AssignToTargetSlot(Actor otherActor)
        {
            UnsetCurrentTargetSlot();
            SurroundManager otherManager = otherActor.GetModule<SurroundManager>();
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
            Actor.MotionVectorsHandler.SetTargetObject(null);
            CurrentTargetSlot = null;
        }

        
        public WorldPoint GetTargetPoint()
        {
            if (CurrentTargetSlot != null)
            {
                return CurrentTargetSlot.GetWorldPoint();
            }
            return CurrentTarget.GetHitPoint(Actor);
        }

        public override void Reset()
        {
            base.Reset();
            CurrentTarget = null;
            if (CurrentTargetSlot != null && CurrentTargetSlot.OccupyingActor == Actor)
            {
                CurrentTargetSlot.OccupyingActor = null;
            }
            CurrentTargetSlot = null;
        }
    }
}