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
        public GameObject Parent;
        public UnityAction OnExecute;
        public UnityAction OnTerminate;
        public bool IsComplete;
        
        public virtual void Execute()
        {
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