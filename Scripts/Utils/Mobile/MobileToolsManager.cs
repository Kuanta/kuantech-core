using Kuantech.Core;
using UnityEngine;
using IngameDebugConsole;
using Lofelt.NiceVibrations;

namespace Kuantech.Utils.Mobile
{
  
    public class MobileToolsManager : SubManager
    {
        #region Haptic Feedback
  
        [Header("Vibrations")]
        [SerializeField] private float VibrationCooldown = 0.2f;
        private float _lastVibrationTime;

        public static void ApplyHaptic(float magnitude, float duration)
        {
#if UNITY_ANDROID || UNITY_IOS
            var context = GetContext<MobileToolsManager>();
            if (context == null)
            {
                Debug.LogWarning("Add Mobile tools manager to apply haptic feedback");
                return;
            }
            if (Time.time - context._lastVibrationTime < context.VibrationCooldown) return;
            context._lastVibrationTime = Time.time;
            HapticPatterns.PlayPreset(HapticPatterns.PresetType.MediumImpact);
#endif

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
        #endregion

    }
}