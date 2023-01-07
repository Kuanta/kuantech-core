using System;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Data;
using UnityEngine;

namespace Kuantech.SurroundSystem
{
    [Serializable]
    public class SurroundSystem
    {
        //Parameters
        public int RowSlotCount; //How many agents on a single row
        public float VerticalDistance = 1f; //Distance to player (or between rows)
        public float HorizontalDistance = 1f;
        
        [SerializeReference] public List<SurroundRow> SurroundSlots = new List<SurroundRow>();
        public List<SurroundAgent> RegisteredSurrounders = new List<SurroundAgent>();

        public Transform Target;

        public Queue<SurroundAgent> QueuedAgents = new Queue<SurroundAgent>();
        public Dictionary<SurroundSlot, SurroundAgent> SlotsToAgentsMap = new Dictionary<SurroundSlot, SurroundAgent>();
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
                if(RegisteredSurrounders[i] == null || !RegisteredSurrounders[i].Available) continue;
                newRegistry.Add(RegisteredSurrounders[i]);
                FindAvailableSlot(RegisteredSurrounders[i]);
            }
            RegisteredSurrounders = newRegistry;
            
            //Check for unoccupied slots
            bool needRecalculate = false;
            List<SurroundSlot> slots = SlotsToAgentsMap.Keys.ToList();
            foreach (var slot in slots)
            {
                if (!SlotsToAgentsMap[slot].Available)
                {
                    SlotsToAgentsMap.Remove(slot);
                    needRecalculate = true;
                }else if (SlotsToAgentsMap[slot].Available && SlotsToAgentsMap[slot].AssignedSlot != slot)
                {
                    SlotsToAgentsMap.Remove(slot);
                    needRecalculate = true;
                    
                }
            }
        }

        public bool IsSlotOccupied(SurroundSlot slot)
        {
            if (!SlotsToAgentsMap.ContainsKey(slot)) return false;
            if (SlotsToAgentsMap[slot].Available) return true;
            SlotsToAgentsMap.Remove(slot);
            return false;
        }
        
        public void RegisterAgent(SurroundAgent agent)
        {
            agent.Available = true;
            QueuedAgents.Enqueue(agent);
        }
        
        public void HandleAgentQueue()
        {
            if (QueuedAgents == null || QueuedAgents.Count == 0) return;
            SurroundAgent agent = QueuedAgents.Dequeue();
            while (agent != null)
            {
                RegisteredSurrounders.Add(agent);
                FindAvailableSlot(agent);
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
            if (agent == null || !agent.Available) return;
            Enums.Directions direction;
            Vector3 diffVector = agent.transform.position - Target.position;
            float angle = Vector3.Angle(Target.forward, diffVector);
            if (angle <= 0)
            {
                direction = Enums.Directions.RIGHT;
            }
            else
            {
                direction = Enums.Directions.LEFT;
            }
            agent.Available = true;
            for(int i=0;i<SurroundSlots.Count; ++i)
            {
                //Agents on a row shouldn't go further away
                SurroundRow currentRow = SurroundSlots[i];
                SurroundSlot candidateSlot = currentRow.FindSuitableSlot(direction);
                
                if(agent.AssignedSlot != null && candidateSlot == agent.AssignedSlot) continue;
       
                //If agent is at an currently optimal position...
                if (agent.AssignedSlot != null && IsSlotOccupied(agent.AssignedSlot) && candidateSlot != null)
                {
                    if (agent != SlotsToAgentsMap[agent.AssignedSlot])
                    {
                        Debug.LogError("ANAYNI BACIYNI");
                    }
                    bool rowCondition = candidateSlot.Row > agent.AssignedSlot.Row;
                    bool colCondition = candidateSlot.Row == agent.AssignedSlot.Row && Mathf.Abs(candidateSlot.Column) >=
                        Mathf.Abs(agent.AssignedSlot.Column);
                    if (rowCondition)
                    {
                        //Don't search for slots on back rows if agent already has a place on front row
                        return;
                    }

                    if (colCondition)
                    {
                        continue;
                    }
                }
                if(candidateSlot == null) continue;
                
                if (SlotsToAgentsMap.ContainsKey(candidateSlot))
                {
                    continue;
                }
                
                //Release its old slot
                if (agent.AssignedSlot != null)
                {
                    SlotsToAgentsMap.Remove(agent.AssignedSlot);
                    agent.AssignedSlot = null;
                }
                agent.AssignToSlot(candidateSlot);
                SlotsToAgentsMap[candidateSlot] = agent;
                return;
            }
            
            //No free slot was available, generate a new row
            SurroundRow newRow = new SurroundRow(SurroundSlots.Count, this);
            newRow.FillSlots(RowSlotCount, HorizontalDistance, VerticalDistance);
            SurroundSlots.Add(newRow);
            
            //Receive a suitable slot according to agents position
            SurroundSlot foundSlot = newRow.FindSuitableSlot(direction);
            agent.AssignToSlot(foundSlot);
            SlotsToAgentsMap[foundSlot] = agent;
        }
        
        public void UnregisterAgent(SurroundAgent agent)
        {
            if (agent == null || agent.AssignedSlot == null) return;
            SlotsToAgentsMap.Remove(agent.AssignedSlot);
            agent.Available = false;
            agent.AssignedSlot = null;
            RecalculateSlots();
        }
        
        public void Cleanup()
        {
            SurroundSlots.Clear();
            RegisteredSurrounders.Clear();
            SlotsToAgentsMap.Clear();
        }
    }
}