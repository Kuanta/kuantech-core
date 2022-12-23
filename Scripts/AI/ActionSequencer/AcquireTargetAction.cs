using Kuantech.ActionSequencer;
using UnityEngine.Events;

namespace Kuantech.Core.AI
{
    public class AcquireTargetAction : SequenceAction
    {
        private Actor Target;

        public UnityAction<Actor> TargetAcquireOverrideHandler;
    }
}