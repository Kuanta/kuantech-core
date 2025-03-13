using UnityEngine;

namespace Kuantech.AI.ActionSequencer
{
    public class DelayAction : SequenceAction
    {
        public float DelayTime;
        private float _startTime;

        public DelayAction()
        {
            DelayTime = 1f;
        }
        public DelayAction(float delayTime)
        {
            DelayTime = delayTime;
        }
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