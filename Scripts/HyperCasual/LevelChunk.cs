using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public class LevelChunk : MonoBehaviour
    {
        public LevelElement[] LevelElements;
        public Level ParentLevel;
        public virtual void OnLevelCreate()
        {
            LevelElements = GetComponentsInChildren<LevelElement>();
            foreach (var element in LevelElements)
            {
                element.InitialPosition = element.transform.position;
                element.InitialRotaiton = element.transform.rotation;
                element.OnLevelCreated();
            }
        }
        
        #region Lifecycle

        public virtual void OnPrepare(Level parentLevel)
        {
            ParentLevel = parentLevel;
            foreach (LevelElement levelElement in LevelElements)
            {
                if(levelElement == null) continue; //todo: Boss chunk has a null level element
                levelElement.ParentLevel = ParentLevel;
                levelElement.OnPrepareLevel();
            }

        }

        public virtual void OnRestart()
        {
            //Restart level elements
            foreach (LevelElement levelElement in LevelElements)
            {
                if(levelElement == null) continue; //todo: Boss chunk has a null level element
                levelElement.OnRestartLevel();
            }
        }
        
        public virtual void OnClear()
        {
            ClearChunk();
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

        public virtual void ClearChunk()
        {
        }
    }
}