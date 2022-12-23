using System;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.ActionSequencer
{
    public struct UpdateEventArgs
    {
        public SequenceAction Action;
        public float DeltaTime;
    }
    
    [Serializable]
    public class SequenceAction
    {
        [NonSerialized] public ActionSequencer Sequencer;
        public GameObject Parent;
        public UnityAction OnExecute;
        public UnityAction OnTerminate;
        public bool IsComplete;
        public bool Disabled = false;
        
        public virtual void Execute()
        {
            if (Disabled)
            {
                IsComplete = true;
                return;
            }
            IsComplete = false;
            OnExecute?.Invoke();
        }

        public virtual void Update(float deltaTime)
        {
  
        }
        
        public virtual void Terminate()
        {
            IsComplete = true;
            OnTerminate?.Invoke();
        }
    }
}