using System;
using UnityEngine;

namespace Kuantech.Core
{
    [Serializable]
    public class AnimationData
    {
        [Header("Animation Timing")]
        public float AnimationTime;
        public string AnimationTimeMultiplierParameterName;
        
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
        }
    }
}