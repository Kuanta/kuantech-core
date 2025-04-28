using UnityEngine;

namespace Kuantech.Core
{
    public class DelayTask : GameTask
    {
        public float DelayDuration;

        private float _startTime;

        public override void StartTask()
        {
            base.StartTask();
            _startTime = Time.time;
        }

        public override void UpdateTask(float deltaTime)
        {
            base.UpdateTask(deltaTime);
            if (Time.time - _startTime >= DelayDuration)
            {
                CompleteTask();
            }
        }
    }
}