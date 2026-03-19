using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core
{
    public class SurroundManager : ActorModule
    {
        [Header("Slot Allocator")] 
        public float SurroundRadius;
        public TargetSlotAllocator SlotAllocator;
        
        [Tooltip("Final slot is calculated by 2*PI*Radius / SurroundSlotsDistance")] 
        public float SurroundSlotsDistance;
        
        [Header("Debug")]
        [SerializeField] private bool DebugSlots;
        [SerializeField] private GameObject SlotDebugIndicator;

        //Runtime
        public TargetSlot CurrentTargetSlot;
        public HashSet<Actor> TargetedByActors = new HashSet<Actor>(); //Actors that targets this actor

        //todo: Create a faction based target system
        private Actor CurrentTarget = null;

  
        public override void OnActorStateChanged(ActorState oldState, ActorState newState)
        {
            if (newState == ActorState.Spawned)
            {
                SetSlotAllocator();
            }
            else if(newState == ActorState.Despawned || newState == ActorState.Dead)
            {
                ClearTarget(); //Clear target just in case
            }
        }

        private void SetSlotAllocator()
        {
            ClearDebugPoints();
            int slotCount = GetSlotCount();
            if (slotCount== 0) SlotAllocator = null;
            else
            {
                if (SlotAllocator != null)
                {
                    //CLear points from earlier
                    ClearDebugPoints();
                }
                
                SlotAllocator = new TargetSlotAllocator(Actor, slotCount, SurroundRadius, Actor.ActorForwardVector, Actor.ActorUpVector);
                SlotAllocator.DebugVisualizationObjectPrefab = SlotDebugIndicator;
            }
        }

        private void ClearDebugPoints()
        {
            if (SlotAllocator == null || SlotAllocator.Slots == null || SlotAllocator.Slots.IsNullOrEmpty()) return;
            foreach (var slot in SlotAllocator.Slots)
            {
                if(slot == null || slot.DebugVisualizationObject == null) continue;
                Helpers.DestroyGameObject(slot.DebugVisualizationObject);
                slot.DebugVisualizationObject = null;
            }
        }
        
        public int GetSlotCount()
        {
            float radius = Mathf.Max(0.1f, Actor.ActorRadius);
            return Mathf.CeilToInt((2 * Mathf.PI * radius) / Mathf.Max(0.1f, SurroundSlotsDistance));
        }
        
        public void SetRadius(float radius)
        {
            //Recalculate slot allocator
            SurroundRadius = radius;
            SetSlotAllocator();
        }
        
        public bool SetCurrentTarget(Actor target)
        {
            ClearTarget();
            if (target != null)
            {
                Actor.MotionVectorsHandler.SetTargetObject(target.transform);
            }
            CurrentTarget = target;
            return true;
        }
        
        /// <summary>
        /// Clears current target
        /// </summary>
        public void ClearTarget()
        {
            ClearTargetedByFromTarget();
            CurrentTarget = null;
            Actor.MotionVectorsHandler.SetTargetObject(null);
        }
        
        /// <summary>
        /// Set targeter actor
        /// </summary>
        /// <param name="targeter"></param>
        public void OnTargetedBy(Actor targeter)
        {
            if (TargetedByActors == null) TargetedByActors = new HashSet<Actor>();
            TargetedByActors.Add(targeter);
        }
        
        /// <summary>
        /// Removes this actor from the targeted by list of the current target, if current target exists
        /// </summary>
        public void ClearTargetedByFromTarget()
        {
            if(CurrentTarget == null) return;
            SurroundManager sm = CurrentTarget.GetModule<SurroundManager>();
            if (sm == null) return;
            if (sm.TargetedByActors != null)
            {
                sm.TargetedByActors.Remove(Actor);
            }
        }
        
        /// <summary>
        /// Tries to assign this actor to a 'melee slot' 
        /// </summary>
        public void AssignToTargetSlot(Actor otherActor, TargetDetectionSlotType detectionSlotType = TargetDetectionSlotType.ByDistance, bool inverted = false)
        {
            UnsetCurrentTargetSlot();
            SurroundManager otherManager = otherActor.GetModule<SurroundManager>();
            TargetSlot slot = null;
            if (otherManager != null && otherManager.SlotAllocator != null)
            {
                slot = otherManager.SlotAllocator.GetBestSlot(Actor, detectionSlotType, inverted);
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

        
        public override void ResetModule()
        {
            base.ResetModule();
            ClearTarget();
            if (CurrentTargetSlot != null && CurrentTargetSlot.OccupyingActor == Actor)
            {
                CurrentTargetSlot.OccupyingActor = null;
            }

            CurrentTargetSlot = null;
            TargetedByActors = null;
            ClearDebugPoints();
        }

        public override void ModuleUpdate()
        {
            base.ModuleUpdate();
            if (DebugSlots && Actor.IsAlive() && SlotAllocator != null)
            {
                SlotAllocator.VisualizeSlots();
            }
        }

        public override void Cleanup()
        {
            ClearDebugPoints();
        }
    }
}