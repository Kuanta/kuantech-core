using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core
{
    public class TargetManager : ActorModule
    {
        [Header("Slot Allocator")]
        public TargetSlotAllocator SlotAllocator;
        public int SlotCount = 0;
        public float Radius;
        
        //Runtime
        public WorldPoint CurrentTargetPoint;
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
            CurrentTarget = target;
            return true;
        }

        public Actor GetCurrentTarget()
        {
            return CurrentTarget;
        }
        
        public void UnsetCurrentTarget()
        {
            CurrentTarget = null;
        }
    }
}