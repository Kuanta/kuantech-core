using UnityEngine;

namespace Kuantech.Core
{
    public class ActorAnimationModule : ActorModule
    {
        public Animator Animator;

        /// <summary>
        /// Rebinds the animator on reset
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            Animator.Rebind();
        }

    }
}