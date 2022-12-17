using System;
using System.Collections.Generic;
using Kuantech.Utils.Enums;
using UnityEngine;

namespace Kuantech.SurroundSystem
{
    [Serializable]
    public class SurroundSystem
    {
        //Parameters
        public int RowSlotCount = 3; //How many agents on a single row
        public float VerticalDistance = 1f; //Distance to player (or between rows)
        public float HorizontalDistance = 1f;
        
        public List<SurroundRow> SurroundSlots = new List<SurroundRow>();
        public List<SurroundAgent> RegisteredSurrounders = new List<SurroundAgent>();

        public Transform Target;

        public Queue<SurroundAgent> QueuedAgents = new Queue<SurroundAgent>();
        public SurroundSystem(Transform target)
        {
            Target = target;
        }
        
        //Recalculates the positions of registered agents
        public void RecalculateSlots()
        {
            List<SurroundAgent> newRegistry = new List<SurroundAgent>(); //To remove disabled agents
            for (int i = 0; i < RegisteredSurrounders.Count; ++i)
            {
                if(!RegisteredSurrounders[i].Available) continue;
                newRegistry.Add(RegisteredSurrounders[i]);
                FindAvailableSlot(RegisteredSurrounders[i]);
            }
            RegisteredSurrounders = newRegistry;
        }

        public void RegisterAgent(SurroundAgent agent)
        {
            QueuedAgents.Enqueue(agent);

        }
        
        public void HandleAgentQueue()
        {
            if (QueuedAgents == null || QueuedAgents.Count == 0) return;
            SurroundAgent agent = QueuedAgents.Dequeue();
            while (agent != null)
            {
                FindAvailableSlot(agent);
                RegisteredSurrounders.Add(agent);
                agent = QueuedAgents.Count > 0 ? QueuedAgents.Dequeue() : null;
            }
            QueuedAgents.Clear();
        }

        public void SetHorizontalOffsets(float horizontalWidth, float normalizedHorizontalPosition)
        {
            foreach (var row in SurroundSlots)
            {
                row.SetHorizontalOffsets(horizontalWidth, normalizedHorizontalPosition);
            }
        }
        private void FindAvailableSlot(SurroundAgent agent)
        {
            Utils.Enums.Directions direction;
            Vector3 diffVector = agent.transform.position - Target.position;
            float angle = Vector3.Angle(Target.forward, diffVector);
            if (angle <= 0)
            {
                direction = Directions.RIGHT;
            }
            else
            {
                direction = Directions.LEFT;
            }
            agent.Available = true;
            for(int i=0;i<SurroundSlots.Count; ++i)
            {
                //Agents on a row shouldn't go further away
                SurroundRow currentRow = SurroundSlots[i];
                SurroundSlot candidateSlot = currentRow.FindSuitableSlot(direction);
                
                if(agent.AssignedSlot != null && candidateSlot == agent.AssignedSlot) continue;
       
                //If agent is at an currently optimal position...
                if (agent.AssignedSlot != null && candidateSlot != null)
                {
                    bool rowCondition = candidateSlot.Row > agent.AssignedSlot.Row;
                    bool colCondition = candidateSlot.Row == agent.AssignedSlot.Row && Mathf.Abs(candidateSlot.Column) >=
                        Mathf.Abs(agent.AssignedSlot.Column);
                    if (rowCondition || colCondition)
                    {
                        return;
                    }
                }
                if(candidateSlot == null) continue;
                //Release its old slot
                if (agent.AssignedSlot != null)
                {
                    agent.AssignedSlot.Occupied = false;
                }
                agent.AssignToSlot(candidateSlot);
                return;
            }
            
            //No free slot was available, generate a new row
            SurroundRow newRow = new SurroundRow(SurroundSlots.Count);
            newRow.FillSlots(RowSlotCount, HorizontalDistance, VerticalDistance);
            SurroundSlots.Add(newRow);
            
            //Receive a suitable slot according to agents position
            agent.AssignToSlot(newRow.FindSuitableSlot(direction));
        }
        
        public void UnregisterAgent(SurroundAgent agent)
        {
            if (agent == null || agent.AssignedSlot == null) return;
            agent.Available = false;
            agent.AssignedSlot.Reset();
            RecalculateSlots();
        }
        
        public void Cleanup()
        {
            SurroundSlots.Clear();
            RegisteredSurrounders.Clear();
        }
    }
}