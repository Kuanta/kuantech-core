using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public enum LevelState
    {
        Waiting,
        Playing,
        Failed,
        Completed,
    }
    
    public class Level : MonoBehaviour
    {
        public LevelState CurrentState;
        public int LevelIndex;
        public List<LevelChunk> LevelChunks;

        public virtual void StartLevel()
        {
            foreach (LevelChunk chunk in LevelChunks)
            {
                chunk.OnPlay();
            }
        }
        
        public virtual void PrepareLevel()
        {
            foreach (LevelChunk chunk in LevelChunks)
            {
                chunk.OnPrepare();
            }
        }

        public virtual void ClearLevel()
        {
            foreach (LevelChunk chunk in LevelChunks)
            {
                chunk.OnClear();
            }
        }

        public virtual void CompleteLevel()
        {
           
        }
    }
}