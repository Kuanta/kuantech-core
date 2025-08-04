using System.Collections.Generic;
using Kuantech.Core.Utils;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.HyperCasual.Runner
{
    public class GeneratedRunnerChunk : RunnerChunk
    {
        private bool _generated = false;
        public List<RunnerChunkElements> RunnerChunkElementPrefabs;
        public PowerLevelArray<GameObject> ChunkElements;
        public override void Initialize(RunnerLevel parentLevel, bool isFinalChunk = false)
        {
            SetChunkElements(parentLevel.LevelNumber);
            base.Initialize(parentLevel, isFinalChunk);
        }

        /// <summary>
        /// Sets the level elements.
        /// </summary>
        /// <param name="powerLevel"></param>
        public void SetChunkElements(int powerLevel)
        {
            ChunkElements = new PowerLevelArray<GameObject>();
            foreach(var prefab in RunnerChunkElementPrefabs)
            {
                ChunkElements.AddElement(prefab.gameObject, prefab.MinLevel, prefab.MaxLevel);
            }
            if(_generated) return;
            _generated = true;
            if (ChunkElements == null)
            {
                return;
            };
            if(ChunkElements.Elements.Count == 0)
            {
                Debug.LogError($"{name} has no elements");
                return;
            }
            GameObject elementsPrefab = ChunkElements.GetRandomElement(powerLevel);
            if (elementsPrefab != null)
            {
                GameObject elements = Instantiate(elementsPrefab);
                RunnerChunkElements rce = elements.GetComponent<RunnerChunkElements>();
                if (rce != null)
                {
                    ChunkDepth = rce.Depth;
                }
                elements.AttachToParent(transform);
            }
        }

    }
}