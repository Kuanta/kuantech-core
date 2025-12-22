using System.Collections.Generic;
using Kuantech.Core.UI;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public class DiscreteFill : MonoBehaviour
    {
        public DiscreteFillChunk ChunkPrefab;
        public Transform ChunksParent;
        public List<DiscreteFillChunk> FillChunks;

        public void Initialize(int chunkAmount)
        {
            ChunksParent.DestroyAllChildren();
            FillChunks = new List<DiscreteFillChunk>();
            for (int i = 0; i < chunkAmount; ++i)
            {
                DiscreteFillChunk chunk = Instantiate(ChunkPrefab.gameObject, ChunksParent).GetComponent<DiscreteFillChunk>();
                FillChunks.Add(chunk);
            }
        }
        
        public void SetFill(int fillAmount)
        {
            fillAmount = Mathf.Clamp(fillAmount, 0, FillChunks.Count);
            for (int i = 1; i <= FillChunks.Count; ++i)
            {
                FillChunks[i - 1].Toggle(i <= fillAmount);
            }
        }
    }
}