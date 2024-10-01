using System;
using Kuantech.Core;

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

        public void ClearState()
        {
            CurrentState = null;
        }
        public override void Reset()
        {
            ClearState();
        }
    }
}