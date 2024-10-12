using System;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Puzzle.Jam
{
    [Serializable]
    public class JamLevelQueueState
    {
        public Dictionary<int, int> CurrentIndices;
    }
    
    [Serializable]
    public class JamLevelQueue<TElementDataType>
    {
        public Dictionary<int, List<TElementDataType>> PuzzleLevelElementStates;
        [Tooltip("If set to true, an empty queue will try to get elements fom other queues")]
        public bool UseFromOtherQueues = false;
        private Dictionary<int, Queue<TElementDataType>> _queues;

        public void SetElementsForIndex(int index, List<TElementDataType> elements)
        {
            if (PuzzleLevelElementStates == null)
            {
                PuzzleLevelElementStates = new Dictionary<int, List<TElementDataType>>();
            }

            PuzzleLevelElementStates[index] = elements;
        }

        public void SetupQueues(JamLevelQueueState state = null)
        {
            
            _queues = new Dictionary<int, Queue<TElementDataType>>();
            foreach (var pair in PuzzleLevelElementStates)
            {
                int startingIndex = 0;
                if (state != null && state.CurrentIndices != null && state.CurrentIndices.ContainsKey(pair.Key))
                {
                    startingIndex = state.CurrentIndices[pair.Key];
                }
                SetupQueueForIndex(pair.Key, startingIndex);
            }
        }
        
        /// <summary>
        /// Sets the queues for a given index/lane. 
        /// </summary>
        /// <param name="index">Index of the queue</param>
        /// <param name="startingIndex">Index of element to start the queue. Used for loaded states</param>
        public void SetupQueueForIndex(int index, int startingIndex)
        {
            if (_queues == null)
            {
                _queues = new Dictionary<int, Queue<TElementDataType>>();
            }
            startingIndex = Mathf.Max(0, startingIndex);
            _queues[index] = new Queue<TElementDataType>();
            for (int i = startingIndex; i < PuzzleLevelElementStates[index].Count; ++i)
            {
                _queues[index].Enqueue(PuzzleLevelElementStates[index][i]);
            }
        }
        public bool IsQueueEmpty(int index)
        {
            bool allQueuesEmpty = true;
            foreach (var pair in _queues)
            {
                if (!pair.Value.IsNullOrEmpty())
                {
                    allQueuesEmpty = false;
                    break;
                }
            }

            if (_queues.ContainsKey(index) && !_queues[index].IsNullOrEmpty())
            {
                return false;
            }

            if (!UseFromOtherQueues) return true; //This queue is empty, If not counting other queues, we can say that this queue is empty

            return allQueuesEmpty; //If counting other queues, check state of all other queues
        }

        public int GetRemainingelementCount(int index)
        {
            return !_queues.ContainsKey(index) ? 0 : _queues[index].Count;
        }

        public int GetElementsCount(int index)
        {
            return PuzzleLevelElementStates.ContainsKey(index) ? PuzzleLevelElementStates[index].Count : 0;
        }
        public int GetQueueIndex(int index)
        {
            return PuzzleLevelElementStates[index].Count - GetRemainingelementCount(index);
        }
        
        public TElementDataType GetNextElementData(int index)
        {
            if (IsQueueEmpty(index)) return GetDefaultData();
            if (_queues.ContainsKey(index) && !_queues[index].IsNullOrEmpty())
            {
                return _queues[index].Dequeue();
            }
            
            //The given index has no other elements
            if (!UseFromOtherQueues)
            {
                return GetDefaultData();
            }
            
            //Try to find a non empty queue and get from it. Prefer the queue with higher count
            int maxKey = _queues.Keys.ElementAt(0);
            int maxCount = _queues[maxKey].Count;
            foreach (var pair in _queues)
            {
                int currentCount = pair.Value.Count;
                if (currentCount > maxCount)
                {
                    maxKey = pair.Key;
                    maxCount = currentCount;
                }
            }

            return _queues[maxKey].Dequeue();

        }

        public TElementDataType GetDefaultData()
        {
            // Check if ElementDataType is nullable or a reference type
            if (typeof(TElementDataType).IsClass || Nullable.GetUnderlyingType(typeof(TElementDataType)) != null)
            {
                return default;
            }
            else
            {
                throw new InvalidOperationException("ElementDataType is a non-nullable value type and cannot be null.");
            }
        }

        public JamLevelQueueState GetState()
        {
            JamLevelQueueState state = new JamLevelQueueState();
            state.CurrentIndices = new Dictionary<int, int>();
            foreach (var pair in _queues)
            {
                int countInQueue = pair.Value.Count;
                int totalCount = PuzzleLevelElementStates[pair.Key].Count;
                state.CurrentIndices[pair.Key] = totalCount - countInQueue;
            }

            return state;
        }
        
        public void Reset()
        {
            SetupQueues(null);
        }
    }
}