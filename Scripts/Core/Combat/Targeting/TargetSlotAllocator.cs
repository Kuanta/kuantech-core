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
        
        public GameObject DebugVisualizationObject;
        
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
        public TargetSlot[] Slots;
        private Actor _parent;
        public Vector3 LocalForward;
        public Vector3 LocalUp;
        
        public GameObject DebugVisualizationObjectPrefab;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        public TargetSlotAllocator(Actor parentActor, int slotCount, float radius, Vector3 localForward, Vector3 localUp)
        {
            _parent = parentActor;
            Slots = new TargetSlot[slotCount];
            LocalUp = localUp;
            LocalForward = localForward;
            for (int i = 0; i < slotCount; i++)
            {
                float angle = (360f / slotCount) * i;
                Vector3 offset = Quaternion.AngleAxis(angle, localUp) * localForward * radius;
                Slots[i] = new TargetSlot
                {
                    Index= i,
                    OffsetPosition = offset,
                    ParentActor = parentActor,
                    OccupyingActor = null,
                    OwnerTargetSlotAllocator = this,
                };
            }
        }

        public void SetRadius(float radius)
        {
            if (Slots == null || Slots.Length == 0) return;
            for (int i = 0; i < Slots.Length; i++)
            {
                float angle = (360f / Slots.Length) * i;
                Vector3 offset = Quaternion.AngleAxis(angle, LocalUp) * LocalForward * radius;
                Slots[i].OffsetPosition = offset;
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
            for(int i=0;i<Slots.Length; i++)
            {
                if (IsSlotOccupied(Slots[i]))
                {
                    //todo: Maybe check the targeted actor of occupying actor. If its null this slot is free
                    continue;
                }

                Vector3 slotPos = Slots[i].GetWorldPoint().GetTargetPosition();
                float score = 1/Vector3.Distance(attackingActor.transform.position, slotPos);
                if (score > bestScore)
                {
                    bestSlotIndex = i;
                    bestScore = score;
                }
            }

            if (bestSlotIndex < 0) return null;
            return Slots[bestSlotIndex];
        }

        public bool IsSlotOccupied(TargetSlot slot)
        {
            Actor occupyingActor = slot.OccupyingActor;
            
            //No alive occupying actor check
            if (occupyingActor == null || !slot.OccupyingActor.IsAlive()) return false;
            
            //Check if occupying actor actually targeting this
            SurroundManager tm = occupyingActor.GetModule<SurroundManager>();
            if (tm == null) return true;
            if (tm.CurrentTargetSlot == slot) return true;
            return false; //Not the same slot
        }
        
        public void RegisterActorToSlot(Actor attackingActor, int slotIndex)
        {
            Slots[slotIndex].OccupyingActor = attackingActor;
        }

        public void RemoveActorFromSlot(int slotIndex)
        {
            Slots[slotIndex].OccupyingActor = null;
        }

        public void VisualizeSlots()
        {
            foreach (var slot in Slots)
            {
                if (slot.DebugVisualizationObject == null)
                {
                    if (DebugVisualizationObjectPrefab == null) return;
                    slot.DebugVisualizationObject = GameObject.Instantiate(DebugVisualizationObjectPrefab);
                }
                slot.DebugVisualizationObject.transform.position = slot.GetWorldPoint().GetTargetPosition();
                var renderer = slot.DebugVisualizationObject.GetComponentInChildren<MeshRenderer>();
                renderer.material.SetColor(BaseColor, IsSlotOccupied(slot) ? Color.red : Color.green);
            }
        }
    }
}