using System;
using Kuantech.Core;
using UnityEngine;
using CandyCoded.HapticFeedback;
using IngameDebugConsole;

namespace Kuantech.Utils.Mobile
{
  
    public class MobileToolsManager : SubManager
    {
        #region Haptic Feedback
        public enum HapticMagnitudes
        {
            Light, Medium, Heavy,
        }
        [Header("Vibrations")]
        [SerializeField] private float VibrationCooldown = 0.2f;
        private float _lastVibrationTime;
        #endregion
        
        public static void ApplyHaptic(HapticMagnitudes mag)
        {
            var context = GetContext<MobileToolsManager>();
            if (context == null)
            {
                Debug.LogWarning("Add Mobile tools manager to apply haptic feedbacl");
                return;
            }
            if (Time.time - context._lastVibrationTime < context.VibrationCooldown) return;
            context._lastVibrationTime = Time.time;
            switch (mag)
            {
                case HapticMagnitudes.Light:
                    HapticFeedback.LightFeedback();
                    break;
                case HapticMagnitudes.Medium:
                    HapticFeedback.MediumFeedback();
                    break;
                case HapticMagnitudes.Heavy:
                    HapticFeedback.HeavyFeedback();
                    break;
            }
        }

        [ConsoleMethod("setHapticCooldown", "Sets haptic feedback cooldown")]
        public static void SetHapticCooldown(float cooldown)
        {
            var context = GetContext<MobileToolsManager>();
            if (context == null)
            {
                return;
            }
            context.VibrationCooldown = cooldown;
        }
    }
}