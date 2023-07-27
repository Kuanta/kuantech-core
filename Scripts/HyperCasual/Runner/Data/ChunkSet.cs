using System;
using System.Collections.Generic;
using Kuantech.Core.Utils;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    [Serializable]
    public struct BaseChunkData
    {
        public ChunkType ChunkType;
        public List<RunnerChunk> Chunks;
    }
    
    [CreateAssetMenu(fileName = "ChunkSet", menuName = "Kuantech/ChunkSet")]
    public class ChunkSet : ScriptableObject
    {
        public List<BaseChunkData> BaseChunks;
        private Dictionary<ChunkType, List<RunnerChunk>> ChunkDictionary;
        public PowerLevelArray<GameObject> ChunkContents;

        public GameObject GetRandomBaseChunk(ChunkType chunkType)
        {
            if(ChunkDictionary == null || ChunkDictionary.Count == 0) InitializeChunkDict();
            return ChunkDictionary?[chunkType].GetRandomElement().gameObject;
        }

        private void InitializeChunkDict()
        {
            ChunkDictionary = new Dictionary<ChunkType, List<RunnerChunk>>();
            foreach (var chunkData in BaseChunks)
            {
                ChunkDictionary[chunkData.ChunkType] = chunkData.Chunks;
            }
        }
    }
}