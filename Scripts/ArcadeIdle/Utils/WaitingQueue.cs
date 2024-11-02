using System.Collections.Generic;
using Kuantech.Utils;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor.TypeSearch;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.ArcadeIdle
{
    public class WaitingQueue : MonoBehaviour
    {
        [Header("Properties")] 
        public int MaxElementCount;
        public float QueuePadding;
        public List<Transform> WaitingPoints;

        public UnityAction<IWaitingQueueElement> OnElementReachedFront;
        
        //Currently spawned entities
        public Queue<IWaitingQueueElement> WaitingElements;
        protected float totalDistance = 0f;

        public virtual void Initialize()
        {
            
        }
        public bool IsQueueFull()
        {
            if (WaitingElements == null) return false;
            return WaitingElements.Count >= MaxElementCount;
        }
        
        /// <summary>
        /// Checks if the queue is empty
        /// </summary>
        /// <returns></returns>
        public bool IsQueueEmpty()
        {
            if (WaitingElements == null) return true;
            return WaitingElements.Count <= 0;
        }
        
        public int GetWaitingElementCount()
        {
            if (WaitingElements == null) return 0;
            return WaitingElements.Count;
        }
        
        /// <summary>
        /// Gets the element waiting to be remvoed
        /// </summary>
        /// <returns></returns>
        public IWaitingQueueElement PeekWaitingElement()
        {
            if (WaitingElements.IsNullOrEmpty()) return null;
            return WaitingElements.Peek();
        }
        
        public virtual void QueueElement(IWaitingQueueElement element, bool updatePositions=true)
        {
            if (IsQueueFull()) return;
            WaitingElements ??= new Queue<IWaitingQueueElement>();
            WaitingElements.Enqueue(element);
            if(updatePositions) UpdateQueuePositions(false);
        }

        public void QueueElements(List<IWaitingQueueElement> elements, bool warpToPositions = false)
        {
            WaitingElements ??= new Queue<IWaitingQueueElement>();
            for (int i = 0; i < elements.Count; ++i)
            {
                WaitingElements.Enqueue(elements[i]);       
            }
            UpdateQueuePositions(warpToPositions);
        }
        
        public IWaitingQueueElement DequeueElement()
        {
            if (WaitingElements.IsNullOrEmpty()) return null;
            IWaitingQueueElement element = WaitingElements.Dequeue();
            UpdateQueuePositions(false);

            if (WaitingElements.Count > 0)
            {
                IWaitingQueueElement newFront = WaitingElements.Peek();
                if (newFront != null)
                {
                    OnElementReachedFront?.Invoke(newFront);
                }
            }
            return element;
        }
        
        [Button("Update Positions")]
        public virtual void UpdateQueuePositions(bool warpToPosition)
        {
            int index = 0;
            totalDistance = 0f;
            if (WaitingElements.IsNullOrEmpty()) return;
            foreach (var actor in WaitingElements)
            {
                WorldPoint worldPoint = GetQueuePosition(index);
                if (worldPoint.Target != null)
                {
                    totalDistance = 0;
                }
                totalDistance += actor.GetSize();
                if (warpToPosition)
                {
                    WarpActorToPosition(actor, worldPoint);
                }
                else
                {
                    SendActorToPosition(actor, worldPoint);
                }
                index++;
            }
        }

        protected virtual void WarpActorToPosition(IWaitingQueueElement actor, WorldPoint worldPoint)
        {
            actor.WarpToPosition(worldPoint);
        }

        protected virtual void SendActorToPosition(IWaitingQueueElement actor, WorldPoint worldPoint)
        {
            actor.GoToPosition(worldPoint);
        }
        /// <summary>
        /// Returns the world point for element at index i in the row
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public virtual WorldPoint GetQueuePosition(int index)
        {
            if (WaitingPoints.Count > index)
            {
                return new WorldPoint()
                {
                    Target = WaitingPoints[index],
                    LocalPosition = Vector3.zero,
                    LocalRotation = Quaternion.identity,
                };
            }

            Transform lastPoint = null;
            int indexOfLast = 0;
            float padding = 0f;
            if (WaitingPoints.IsNullOrEmpty())
            {
                lastPoint = transform;
                indexOfLast = WaitingPoints.Count - 1;
            }
            else
            {
                padding = QueuePadding;
                lastPoint = WaitingPoints[^1];
            }

            Vector3 globalPosition = lastPoint.transform.position -
                                     lastPoint.transform.forward * (totalDistance+padding);
            
            return new WorldPoint()
            {
                Target = null,
                Position = globalPosition,
                Rotation = lastPoint.rotation,
            };
        }

        public void ClearQueue()
        {
            WaitingElements?.Clear();
            totalDistance = 0f;
        }
    }
}