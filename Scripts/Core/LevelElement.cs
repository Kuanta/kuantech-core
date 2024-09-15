using UnityEngine;

namespace Kuantech.Core
{
    /// <summary>
    /// A level component is a components that has lifecycles which are managed by the level
    /// </summary>
    public class LevelElement : MonoBehaviour
    {
        public Level ParentLevel;
        public int ElementId=0;
        public virtual void OnSetupLevel()
        {
            
        }
        /// <summary>
        /// Called at the very start of Level's Play method
        /// </summary>
        public virtual void OnPrePlayLevel()
        {
            
        }
        
        /// <summary>
        /// Called after level Play method done what it needs to do
        /// </summary>
        public virtual void OnPostPlayLevel()
        {
            
        }
        public virtual void OnCompleteLevel()
        {
            
        }

        public virtual void OnFailLevel()
        {
            
        }
        
        #region State

        public virtual void LoadElementState(byte[] state)
        {
            
        }

        public virtual byte[] GetElementState()
        {
            return null;
        }
        #endregion
        
        public virtual void Reset()
        {
            
        }
    }
}