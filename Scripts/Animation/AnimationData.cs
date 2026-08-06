using System;
using UnityEngine;

namespace Kuantech.Core
{
    [Serializable]
    public class AnimationData
    {
        [Header("Animation Timing")]
        public float AnimationTime;
        public string AnimationTimeParameterName;
        
        [Header("Play By State")] 
        public string AnimationStateName;
        public int StateLayer = 0;
        
        [Header("Float")]
        public string FloatParameterName;
        public float FloatParameterValue;

        [Header("Integet")] 
        public string IntegerParameterName;
        public int IntegerParameterValue;
        
        [Header("Boolean")]
        public string BoolParemeterName;
        public bool BoolParameterValue;

        [Header("Trigger")]
        public string TriggerParameterName;
        
        [Header("Animation Montage")]
        public AnimationMontage AttackMontage;

        public void SetParameters(Animator animator)
        {
            if (animator == null) return;

            if (!string.IsNullOrEmpty(FloatParameterName))
            {
                animator.SetFloat(FloatParameterName, FloatParameterValue);
            }
            
            if (!string.IsNullOrEmpty(BoolParemeterName))
            {
                animator.SetBool(BoolParemeterName, BoolParameterValue);
            }

            if (!string.IsNullOrEmpty(TriggerParameterName))
            {
                animator.SetTrigger(TriggerParameterName);
            }

            if (!string.IsNullOrEmpty(IntegerParameterName))
            {
                animator.SetInteger(IntegerParameterName, IntegerParameterValue);
            }

            if (!string.IsNullOrEmpty(AnimationTimeParameterName))
            {
                animator.SetFloat(AnimationTimeParameterName, AnimationTime);
            }
        }

        /// <summary>
        /// Same writes, aimed at an <see cref="IAnimationDriver"/> instead of an Animator, for actors that
        /// have no Animator to write to. Names are hashed here because the driver interface takes hashes —
        /// the callers of the Animator overload already had them, this one does not.
        /// </summary>
        public void SetParameters(IAnimationDriver driver)
        {
            if (driver == null) return;

            if (!string.IsNullOrEmpty(FloatParameterName))
            {
                driver.SetFloat(Animator.StringToHash(FloatParameterName), FloatParameterValue);
            }

            if (!string.IsNullOrEmpty(BoolParemeterName))
            {
                driver.SetBool(Animator.StringToHash(BoolParemeterName), BoolParameterValue);
            }

            if (!string.IsNullOrEmpty(TriggerParameterName))
            {
                driver.SetTrigger(Animator.StringToHash(TriggerParameterName));
            }

            if (!string.IsNullOrEmpty(IntegerParameterName))
            {
                driver.SetInteger(Animator.StringToHash(IntegerParameterName), IntegerParameterValue);
            }

            if (!string.IsNullOrEmpty(AnimationTimeParameterName))
            {
                driver.SetFloat(Animator.StringToHash(AnimationTimeParameterName), AnimationTime);
            }
        }
    }
}