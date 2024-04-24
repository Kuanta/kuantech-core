using System;
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

    public class PuzzleLevelElement : MonoBehaviour 
    {
        [NonSerialized] public PuzzleLevelElementState CurrentState = null;
        public virtual void OnSetup()
        {

        }

        public virtual void OnPlay()
        {

        }

        public virtual void OnRestart()
        {

        }
        public virtual void LoadElementState(byte[] serializedState)
        {
            CurrentState = Helpers.Deserialize<PuzzleLevelElementState>(serializedState);
        }

        public PuzzleLevelElementState GetElementState()
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