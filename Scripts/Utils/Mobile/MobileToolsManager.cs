using System.Collections;
using System.Collections.Generic;
using Kuantech.Core;
using UnityEngine;
using IngameDebugConsole;

#if ENABLE_UNITYHAPTICS
using Lofelt.NiceVibrations;
#endif

namespace Kuantech.Utils.Mobile
{
    public struct HapticPlayData
    {
        public float Intensity;
        public float Magitude;
        public float Duration;
    }
    
    public class MobileToolsManager : SubManager
    {
        #region Haptic Feedback
  
        [Header("Vibrations")]
        [SerializeField] private float VibrationCooldown = 0.2f;
        private Queue<HapticPlayData> HapticQueue;
        private float _lastVibrationTime;
        private bool _isHapticsPlaying = false;
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

            if (!context.HapticsToggled) return;

            context.PlayHapticEffect(new HapticPlayData()
            {
                Magitude = magnitude,
                Intensity = frequency,
                Duration = duration,
            });
#endif

        }

        private void PlayHapticEffect(HapticPlayData data)
        {
            //Use queue
            HapticQueue ??= new Queue<HapticPlayData>();
            if (Time.time -_lastVibrationTime < VibrationCooldown)
            {
                return;
            }
            
            _lastVibrationTime = Time.time;
            Debug.Log("Applied Haptics");
            HapticQueue.Enqueue(data);
            if (!_isHapticsPlaying)
            {
                StartCoroutine(ProcessQueue());
            }
        }

        private IEnumerator ProcessQueue()
        {
            _isHapticsPlaying = true;
            while (HapticQueue.Count > 0)
            {
                HapticPlayData palyData = HapticQueue.Dequeue();
                HapticPatterns.PlayConstant(palyData.Magitude, palyData.Intensity, palyData.Duration);
                yield return new WaitForSeconds(palyData.Duration);
            }
            _isHapticsPlaying = false;
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