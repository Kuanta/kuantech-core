using Kuantech.Core.FX;
using UnityEngine;

namespace Kuantech.Core
{
    public class TriggerAnimationFxBehaviour : FxBehaviour
    {
        public AnimationData AnimationData;
        public Animator Animator;
        public float Cooldown;
        private float _lastPlayed;

        protected override void OnFxStarted(Effect parentFx)
        {
            if(Time.time - _lastPlayed < Cooldown) return;
            AnimationData.SetParameters(Animator);
        }
    }
}