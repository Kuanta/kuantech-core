using System;
using System.Collections.Generic;
using Kuantech.Core.Utils;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public class RunnerChunk : MonoBehaviour
    {
        [Header("Chunk Properties")] 
        public float ChunkWidth;
        public float ChunkDepth;
        
        [Header("Chunk Elements")]
        [SerializeField] private CollisionEventsRelayer EnterGateTrigger;
        [SerializeField] private CollisionEventsRelayer ExitGateTrigger;
        [SerializeField] private Transform AttachPoint;
        [SerializeField] private Transform OriginPoint;
        
        public RunnerLevel ParentLevel;
        private ChunkFormat _chunkFormat;
        
        public void Initialize(RunnerLevel parentLevel, ChunkFormat chunkFormat)
        {
            ParentLevel = parentLevel;
            EnterGateTrigger.OnTriggerEnterEvent += OnRunnerEnter;
            ExitGateTrigger.OnTriggerExitEvent += OnRunnerExit;
            GenerateChunk(chunkFormat);
        }

        private void GenerateChunk(ChunkFormat chunkFormat)
        {
 
            RunnerLevelManager rlm = ((HCGameManager)HCGameManager.Instance).GetSubManagerByType<RunnerLevelManager>() 
                as RunnerLevelManager;
            
            //Layers
            if (chunkFormat.Layers == null) return;
            foreach (var layerFormat in chunkFormat.Layers)
            {
                List<List<string>> slots;
                List<List<float>> rowOffsets = layerFormat.RowOffsets;
                List<List<float>> columnOffsets = layerFormat.ColumnOffsets;
                if (rlm.LevelDesigns.PremadeLayers != null && layerFormat.PremadeKey != null && rlm.LevelDesigns.PremadeLayers.ContainsKey(layerFormat.PremadeKey))
                {
                    slots = new List<List<string>>(rlm.LevelDesigns.PremadeLayers[layerFormat.PremadeKey]); //Copy so that premade ones not overriden
                }
                else
                {
                    slots = layerFormat.Slots;
                }

                if (layerFormat.VerticalScale < 0)
                {
                    slots.Reverse();
                }
                SetChunkLayer(rlm, slots, rowOffsets:rowOffsets, columnOffsets,layerFormat.CustomParameters, layerFormat.HorizontalScale);
            }
        }
        
         private void SetChunkLayer(RunnerLevelManager rlm, List<List<string>> slots , List<List<float>> rowOffsets, 
            List<List<float>> columnOffsets, List<List<string>> customParameters, int horizontalScale= 1)
        {
            //Assign slots
            if(slots == null || slots.Count == 0) return;
            int rowCount = slots.Count;
            for (int r = 0; r < rowCount; ++r)
            {
                List<string> row = new List<string>(slots[r]); //Copy so that premade ones not overriden
                if (horizontalScale < 0)
                {
                    row.Reverse();
                }

              
                int columnsCount = row.Count;
                for (int c = 0; c < columnsCount; ++c)
                {
                    string chunkKeyRaw = row[c];
                    string[] splitKey = chunkKeyRaw.Split(';');
                    string chunkKey = splitKey[0]; 
                    
                    float rowOffset = 0;
                    if (rowOffsets != null && rowOffsets.Count > r &&  rowOffsets[r].Count > c)
                    {
                        rowOffset = rowOffsets[r][c];
                    }

                    float columnOffset = 0;
                    if (columnOffsets != null && columnOffsets.Count > r && columnOffsets[r].Count > c)
                    {
                        columnOffset = columnOffsets[r][c];
                    }
                    
                    //Get chunk prefab
                    if (!rlm.SlotPrefabs.ContainsKey(chunkKey))
                    {
                        continue;
                    }

                    GameObject slotObject = RunnerLevelManager.InstantiateLevelElement(rlm.SlotPrefabs[chunkKey]);
                    
                    if (slotObject == null)
                    {
                        Debug.LogError("Null slot object");
                        continue;
                    }
                    AssignToSlot(slotObject, rowCount, columnsCount, r, c, rowOffset:rowOffset, columnOffset:columnOffset);

                    //Try to get custom parameter
                    string customParameter = null;
                    if (splitKey.Length > 1)
                    {
                        //First, check if argument is embedded into the chunk key
                        customParameter = splitKey[1];
                    }
                    else
                    {
                        //Check custom parameters list
                        if (!(customParameters == null || customParameters.Count <= r || customParameters[r] == null ||
                            customParameters[r].Count <= c))
                        {
                            customParameter = customParameters[r][c];
                        }
                    }

                    if (slotObject.TryGetComponent(out RunnerChunkSlot chnkSlot))
                    {
                        chnkSlot.OnAssign(this, r, c, rowCount, columnsCount, customParameter);
                    }
                }
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


        public void ClearChunk()
        {
            
        }
        
        #region Triggers

        public void OnRunnerEnter(object sender, Collider other)
        {
        }
        
        public void OnRunnerExit(object sender, Collider other)
        {
            if (!other.TryGetComponent(out Runner runner)) return;
            ParentLevel.OnPlayerExitChunk(this);
        }
        #endregion
    }
}