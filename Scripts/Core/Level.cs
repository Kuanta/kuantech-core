using UnityEngine;

namespace Kuantech.Core
{
    public enum LevelState
    {
        Waiting = 0,
        Playing,
        Completed,
        Failed,
    }

    public class Level : MonoBehaviour
    {
        public int LevelIndex;
        public int PowerLevel;
        public LevelState CurrentState;

        #region Level Lifecycle
        public virtual void SetupLevel()
        {

        }

        public virtual void PlayLevel()
        {

        }

        public virtual void CompleteLevel()
        {

        }

        public virtual void FailLevel()
        {

        }
        public virtual void RestartLevel()
        {
            

        }

        public virtual void ClearLevel()
        {

        }
        #endregion
    }
}