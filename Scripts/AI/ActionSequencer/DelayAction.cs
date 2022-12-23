using Kuantech.ActionSequencer;
using UnityEngine;

namespace Kuantech.Core.AI.ActionSequencer
{
    public class DelayAction : SequenceAction
    {
        public float DelayTime;
        private float _startTime;
        public override void Execute()
        {
            base.Execute();
            _startTime = Time.time;
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            if (Time.time - _startTime >= DelayTime) IsComplete = true;
        }
    }
}