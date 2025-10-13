using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core
{
    public class TargetSlot
    {
        public int Index;
        //public WorldPoint WorldPoint;
        public Vector3 OffsetPosition;
        public Actor OccupyingActor;
        public Actor ParentActor;
        public TargetSlotAllocator OwnerTargetSlotAllocator;

        public WorldPoint GetWorldPoint()
        {
            return new WorldPoint()
            {
                Target = ParentActor.GetActorAnchor(),
                OffsetPosition = OffsetPosition,
            };
        }
    }
    
    public class TargetSlotAllocator
    {
        private TargetSlot[] _slots;
        private Actor _parent;
        public Vector3 LocalForward;
        public Vector3 LocalUp;
        
        public TargetSlotAllocator(Actor parentActor, int slotCount, float radius, Vector3 localForward, Vector3 localUp)
        {
            _parent = parentActor;
            _slots = new TargetSlot[slotCount];
            LocalUp = localUp;
            LocalForward = localForward;
            for (int i = 0; i < slotCount; i++)
            {
                float angle = (360f / slotCount) * i;
                Vector3 offset = Quaternion.AngleAxis(angle, localUp) * localForward * radius;
                _slots[i] = new TargetSlot
                {
                    Index= i,
                    OffsetPosition = offset,
                    ParentActor = parentActor,
                    OccupyingActor = null,
                    OwnerTargetSlotAllocator = this,
                };
            }
        }
        
        /// <summary>
        /// Gets the closest slot
        /// </summary>
        /// <param name="attackingActor"></param>
        /// <returns></returns>
        public TargetSlot GetBestSlot(Actor attackingActor)
        {
            float bestScore = float.MinValue;
            int bestSlotIndex = -1;
            for(int i=0;i<_slots.Length; i++)
            {
                if (IsSlotOccupied(_slots[i]))
                {
                    //todo: Maybe check the targeted actor of occupying actor. If its null this slot is free
                    continue;
                }

                Vector3 slotPos = _slots[i].GetWorldPoint().GetTargetPosition();
                float score = 1/Vector3.Distance(attackingActor.transform.position, slotPos);
                if (score > bestScore)
                {
                    bestSlotIndex = i;
                    bestScore = score;
                }
            }

            if (bestSlotIndex < 0) return null;
            return _slots[bestSlotIndex];
        }

        public bool IsSlotOccupied(TargetSlot slot)
        {
            Actor occupyingActor = slot.OccupyingActor;
            
            //No alive occupying actor check
            if (occupyingActor == null || !slot.OccupyingActor.IsAlive()) return false;
            
            //Check if occupying actor actually targeting this
            TargetManager tm = occupyingActor.GetModule<TargetManager>();
            if (tm == null) return true;
            if (tm.CurrentTargetSlot == slot) return true;
            return false; //Not the same slot
        }
        
        public void RegisterActorToSlot(Actor attackingActor, int slotIndex)
        {
            _slots[slotIndex].OccupyingActor = attackingActor;
        }

        public void RemoveActorFromSlot(int slotIndex)
        {
            _slots[slotIndex].OccupyingActor = null;
        }
    }
}