using Kuantech.Core;
using UnityEngine;
using IngameDebugConsole;

#if ENABLE_UNITYHAPTICS
using Lofelt.NiceVibrations;
#endif

namespace Kuantech.Utils.Mobile
{
  
    public class MobileToolsManager : SubManager
    {
        #region Haptic Feedback
  
        [Header("Vibrations")]
        [SerializeField] private float VibrationCooldown = 0.2f;
        private float _lastVibrationTime;
        public bool HapticsToggled;

        public static void ApplyHaptic(float magnitude, float frequency, float duration)
        {
#if (UNITY_ANDROID || UNITY_IOS) && ENABLE_UNITYHAPTICS
            var context = GetContext<MobileToolsManager>();
            if (context == null)
            {
                Debug.LogWarning("Add Mobile tools manager to apply haptic feedback");
                return;
            }

            if (context.HapticsToggled) return;
            if (Time.time - context._lastVibrationTime < context.VibrationCooldown)
            {
                return;
            }
            
            context._lastVibrationTime = Time.time;
            HapticPatterns.PlayConstant(magnitude, frequency, duration);
#endif

        }

        public static void ToggleHaptics(bool toggle)
        {
            var context = GetContext<MobileToolsManager>();
            if (context == null)
            {
                Debug.LogWarning("Add Mobile tools manager to apply haptic feedback");
                return;
            }
            context.HapticsToggled = toggle;
        }
#if ENABLE_UNITYHAPTICS
        public static void ApplyHaptic(HapticClip clip)
        {
            HapticController.Play(clip);
        }
#endif
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
        #endregion

    }
}