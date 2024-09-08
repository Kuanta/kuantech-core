using System;
using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Puzzle
{
    [Serializable]
    public class PuzzleLevelElementState
    {
        public bool isActive;
        [NonSerialized] public bool Dirtied;
    }

    public class PuzzleLevelElement : LevelElement
    {
        [NonSerialized] public PuzzleLevelElementState CurrentState = null;

        public virtual void OnStageCompleted()
        {
            
        }
        
        public virtual void LoadElementState(byte[] serializedState)
        {
            CurrentState = Helpers.Deserialize<PuzzleLevelElementState>(serializedState);
        }
        
        public virtual PuzzleLevelElementState GetElementState()
        {
            return new PuzzleLevelElementState()
            {
                isActive = isActiveAndEnabled,
            };
        }  

        public virtual PuzzleLevelElementState CreateState()
        {
            CurrentState = new PuzzleLevelElementState();
            return CurrentState;
        }

        public int GetUniqueId()
        {
            return GetInstanceID();
        }

        public void DirtyState()
        {
            if(CurrentState == null)
            {
                return;
            }
            CurrentState.Dirtied = true;
         
        }
    }
}