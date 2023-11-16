using System;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Core.Utils;
using UnityEngine;

namespace Kuantech.Core.HyperCasual.Runner
{
    public class RunnerChunk : LevelChunk
    {
        [Header("Chunk Properties")] 
        public float ChunkWidth;
        public float ChunkDepth;
        
        [Header("Chunk Elements")]
        [SerializeField] private CollisionEventsRelayer EnterGateTrigger;
        [SerializeField] private CollisionEventsRelayer ExitGateTrigger;
        [SerializeField] private Transform AttachPoint;
        [SerializeField] private Transform OriginPoint;
        
        private HashSet<IChunkElement> ChunkElements = new HashSet<IChunkElement>();
        private Queue<IChunkElement> _elementsToAdd = new Queue<IChunkElement>();

        private bool _chunkCompleted = false;
        public bool IsFinalChunk;
        public virtual void Initialize(RunnerLevel parentLevel, bool isFinalChunk=false)
        {
            ParentLevel = parentLevel;
            if(EnterGateTrigger != null) EnterGateTrigger.OnTriggerEnterEvent += OnEnterGateTriggered;
            if(ExitGateTrigger != null) ExitGateTrigger.OnTriggerExitEvent += OnExitGateTriggered;
            _chunkCompleted = false;
            IsFinalChunk = isFinalChunk;
            
            FindChunkElements();
            //Some elements may spawn additional elements (procedural generation). 
            // In order for everything to work perfectly, these runtime spawns should be done before OnChunkGenerated
            foreach (var element in ChunkElements)
            {
                element.OnPreChunkGenerated(this);
            }

            //...Since new elements may have been created, re-find the chunk elements
            FindChunkElements();
            foreach (var element in ChunkElements)
            {
                element.OnChunkGenerated(this);
            }

            while (_elementsToAdd.Count > 0)
            {
                ChunkElements.Add(_elementsToAdd.Dequeue());
            }
        }

        private void FindChunkElements()
        {
            //Some slots doesn't have a script attached but still contains IChunkElement. Get them with get components
            HashSet<IChunkElement> newChunkElements = GetComponentsInChildren<IChunkElement>().ToHashSet(); //Call this after generating
            ChunkElements ??= new HashSet<IChunkElement>();
            ChunkElements.Clear();
            foreach (var newChunkElement in newChunkElements)
            {
                if (newChunkElement == null) continue;
                ChunkElements.Add(newChunkElement);
            }
        }
        public override void OnPrepare(Level parentLevel)
        {
            base.OnPrepare(parentLevel);
            if(EnterGateTrigger != null) EnterGateTrigger.OnTriggerEnterEvent += OnEnterGateTriggered;
            if(ExitGateTrigger != null) ExitGateTrigger.OnTriggerExitEvent += OnExitGateTriggered;
            _chunkCompleted = false;
        }
        
        
        public override void OnRestart()
        {
            base.OnRestart();
            if(EnterGateTrigger != null) EnterGateTrigger.gameObject.SetActive(true);
            if(ExitGateTrigger != null) ExitGateTrigger.gameObject.SetActive(true);
            _chunkCompleted = false;
            foreach (var element in ChunkElements)
            {
                element.OnChunkRestart();
            }
        }

        public override void ClearChunk()
        {
            base.ClearChunk();
            foreach (var element in ChunkElements)
            {
                element.OnClearChunk();
            }
        }
        /// <summary>
        /// Some IChunkElements may create additional IChunkElements. During the initialize, we have to add these new elements
        /// to the ChunkElements. Instead of directly altering the array, we store them in a queue and add them to ChunkElements
        /// after all is done.
        /// </summary>
        /// <param name="element"></param>
        public void AddChunkElement(IChunkElement element, bool useQueue = true)
        {
            if (useQueue)
            {
                _elementsToAdd ??= new Queue<IChunkElement>();
                _elementsToAdd.Enqueue(element);
            }
            else
            {
                ChunkElements.Add(element);
            }

        }
         
         public void AssignToSlot(GameObject slot, int rowCount, int columnCount, int row, int col, float rowOffset=0, float columnOffset=0)
         {
             float depthDelta = ChunkDepth / (rowCount-1 + 1);
             float widthDelta= ChunkWidth / (columnCount-1 + 1);
             //Get Row position
             float rowPos = depthDelta*0.5f + row * depthDelta + depthDelta*rowOffset;
             float widthPos = widthDelta*0.5f + col * widthDelta - ChunkWidth * 0.5f + columnOffset * widthDelta; 

             Vector3 localPos = new Vector3(widthPos, 0f, rowPos);
             slot.transform.SetParent(transform);
             slot.transform.localPosition = localPos;
             slot.transform.localRotation = Quaternion.identity;
         }
        
         public void AttachNewChunk(RunnerChunk newChunk)
        {
            if (AttachPoint == null)
            {
                throw new Exception("Attach point is null for the chunk");
                return;
            }

            newChunk.transform.forward = AttachPoint.forward;
            
            Vector3 offset = Vector3.zero;
            if (newChunk.OriginPoint != null)
            {
                offset = newChunk.OriginPoint.localPosition;
            }

            newChunk.transform.position = AttachPoint.position + offset;
        }

         public void CompleteChunk()
         {
             if (_chunkCompleted) return;
             _chunkCompleted = true;
             if (IsFinalChunk)
             {
                 ParentLevel.CompleteLevel();
             }
         }
        
        #region Triggers

        private void OnEnterGateTriggered(object sender, Collider other)
        {
            if (!other.TryGetComponent(out Runner runner)) return;
            OnRunnerEnter(runner);
        }
        
        private void OnExitGateTriggered(object sender, Collider other)
        {
            if (!other.TryGetComponent(out Runner runner)) return;
            OnRunnerExit(runner);
            
        }

        protected virtual void OnRunnerEnter(Runner runner)
        {
            foreach (var chunkElement in ChunkElements)
            {
                chunkElement.OnPlayerEnteredChunk();
                EnterGateTrigger.gameObject.SetActive(false);
            }
            ((RunnerLevel)ParentLevel).OnPlayerEnterChunk(this);
        }

        protected virtual void OnRunnerExit(Runner runner)
        {
            foreach (var chunkElement in ChunkElements)
            {
                chunkElement.OnPlayerEnteredChunk();
                ExitGateTrigger.gameObject.SetActive(false);
            }
            ((RunnerLevel)ParentLevel).OnPlayerExitChunk(this);
        }
        #endregion

    }
}