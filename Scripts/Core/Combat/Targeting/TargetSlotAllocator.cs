using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core
{
    public class TargetSlot
    {
        public WorldPoint WorldPoint;
        public bool IsOccupied;
        public Actor OccupyingActor;
    }
    
    public class TargetSlotAllocator
    {
        private TargetSlot[] _slots;
        private Transform _parent;
        public Vector3 LocalForward;
        public Vector3 LocalUp;
        
        public TargetSlotAllocator(Transform parent, int slotCount, float radius, Vector3 localForward, Vector3 localUp)
        {
            _parent = parent;
            _slots = new TargetSlot[slotCount];
            LocalUp = localUp;
            LocalForward = localForward;
            for (int i = 0; i < slotCount; i++)
            {
                float angle = (360f / slotCount) * i;
                Vector3 offset = Quaternion.AngleAxis(angle, localUp) * localForward * radius;
                _slots[i] = new TargetSlot
                {
                    WorldPoint = new WorldPoint()
                    {
                        OffsetPosition = offset, //If we want to rotate the slots with the parent actor, use LocalPosition
                        Target = parent,
                    },
                    IsOccupied = false,
                    OccupyingActor = null
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
                if (_slots[i].OccupyingActor != null)
                {
                    //todo: Maybe check the targeted actor of occupying actor
                    continue;
                }

                Vector3 slotPos = _slots[i].WorldPoint.GetTargetPosition();
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

        public void RegisterActorToSlot(Actor attackingActor, int slotIndex)
        {
            _slots[slotIndex].OccupyingActor = attackingActor;
        }
    }
}