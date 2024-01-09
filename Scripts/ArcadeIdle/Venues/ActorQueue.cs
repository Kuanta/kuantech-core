using System;
using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
   
    [Serializable]
    public class ActorQueue : ResourceStacker
    {
        private Queue<ArcadeIdleNpc> _queue = new Queue<ArcadeIdleNpc>();
        public int MaxNpc;
        
        public void QueueActor(ArcadeIdleNpc actor)
        {
            if (_queue.Count >= MaxNpc) return;
            _queue.Enqueue(actor);
            SetActorPosition(actor, _queue.Count - 1);
        }

        public ArcadeIdleNpc DequeueActor()
        {
            if (_queue == null || _queue.Count == 0) return null;
            ArcadeIdleNpc dequed = _queue.Dequeue();
            RepositionActors();
            return dequed;
        }
        
        /// <summary>
        /// Removes from 
        /// </summary>
        /// <param name="npc"></param>
        public void RemoveFromQueue(ArcadeIdleNpc npc)
        {
            Helpers.RemoveFromQueue(_queue, npc);
            npc.AssignedQueue = null;
            RepositionActors();
        }
        
        public bool IsAvailable()
        {
            return MaxNpc > _queue.Count;
        }

        public void RepositionActors()
        {
            int index = 0;
            foreach (var actor in _queue)
            {
                SetActorPosition(actor, index);
                index++;
            }
        }

        private void SetActorPosition(ArcadeIdleNpc npc, int index)
        {
            WorldPoint worldPoint = GetWorldPosition(index);
            worldPoint.Rotation = Quaternion.LookRotation(-1 * AnchorPoint.forward);
            npc.SetDestination(worldPoint);
            npc.ToggleMovement(true);
        }

        /// <summary>
        /// Returns the number of actors in the queue
        /// </summary>
        /// <returns></returns>
        public int GetActorInQueueCount()
        {
            if(_queue == null) return 0;
            return _queue.Count;
        }
    }
}