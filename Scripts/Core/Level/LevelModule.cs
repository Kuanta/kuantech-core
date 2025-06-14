using System;
using UnityEngine;

namespace Kuantech.Core
{
    public class LevelModule : MonoBehaviour
    {
        [NonSerialized] public Level ParentLevel;

   
        public virtual void Initialize()
        {
            
        }

        public virtual void PostLevelSetup()
        {
            
        }
        
        public virtual void OnLevelStateChange(LevelStateChangeData levelStateChangeData)
        {
            
        }

        public virtual void OnLevelPhaseChange(LevelPhase oldPhase, LevelPhase newPhase)
        {
            
        }

        public virtual void OnReset()
        {
         
        }
        public virtual void OnLevelClear()
        {
            
        }
    }
}