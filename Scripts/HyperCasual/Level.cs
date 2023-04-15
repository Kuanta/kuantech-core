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
        public virtual void StartLevel()
        {
            
        }
        
        public virtual void PrepareLevel()
        {
            
        }

        public virtual void ClearLevel()
        {
            
        }

        public virtual void CompleteLevel()
        {
            
        }
    }
}