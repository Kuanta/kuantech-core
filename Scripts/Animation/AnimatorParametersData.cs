using System;
using UnityEngine;

namespace Kuantech.Core
{
    [Serializable]
    public class AnimatorParametersData
    {
        [Header("Float")]
        public string FloatParameterName;
        public float FloatParameterValue;

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
        }
    }
}