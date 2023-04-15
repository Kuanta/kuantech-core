using Kuantech.EndlessRunner;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public class LevelChunk : MonoBehaviour
    {
        public LevelElement[] LevelElements;
        
        public virtual void OnLevelCreate()
        {
            LevelElements = GetComponentsInChildren<LevelElement>();
           
        }
        
        #region Lifecycle

        public virtual void OnPrepare()
        {
            foreach (LevelElement levelElement in LevelElements)
            {
                if(levelElement == null) continue; //todo: Boss chunk has a null level element
                levelElement.OnPrepareLevel();
            }

        }

        public virtual void OnClear()
        {
            foreach (LevelElement levelElement in LevelElements)
            {
                if(levelElement == null) continue;
                levelElement.OnLeaveLevel();
            } 
        }

        public virtual void OnPlay()
        {
            foreach (LevelElement levelElement in LevelElements)
            {
                if(levelElement == null) continue; //todo: Boss chunk has a null level element
                levelElement.OnPlayLevel();
            }

        }

        #endregion
        
        public virtual void OnPlayerEntered()
        {
            foreach (LevelElement levelElement in LevelElements)
            {
                if(levelElement == null) continue;
                levelElement.OnPlayerEntered();
            }
        
        }

        public virtual void OnPlayerExit()
        {
            foreach (LevelElement levelElement in LevelElements)
            {
                if(levelElement == null) continue;
                levelElement.OnPlayerExited();
            }
        }
    }
}