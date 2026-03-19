using Kuantech.Core;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    public class ArcadeIdleAnimator : AnimationModule
    {
        public float MaxSpeed = 5.0f;
        private static readonly int Speed = Animator.StringToHash("Speed");

        public void SetSpeed(float speed)
        {
            float normalizedSpeed = speed / MaxSpeed;
            Animator.SetFloat(Speed, normalizedSpeed);
        }

        public override void Initialize()
        {
        }

        public override void ResetModule()
        {
        }
        
        #region Arcade Idle animationss
        
        #endregion
    }
}