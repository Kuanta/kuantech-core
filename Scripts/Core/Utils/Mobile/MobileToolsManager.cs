using System.Collections;
using System.Collections.Generic;
using Kuantech.Core;
using UnityEngine;
using IngameDebugConsole;
using Sirenix.OdinInspector;
using UnityEditor;

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
        public float DefaultHapticMagnitude = 1;
        public float DefaultHapticDuration = 0.1f;
        public float DefaultHapticFrequency = 1;
        [SerializeField] private float VibrationCooldown = 0.2f;
        private Queue<HapticPlayData> HapticQueue;
        private float _lastVibrationTime;
        private bool _isHapticsPlaying = false;
        public bool HapticsToggled;
        
        public override void OnSubmanagersInitialized()
        {
            base.OnSubmanagersInitialized();
            DefaultHapticMagnitude = ConfigManager.GetFloatConfig("HapticMagnitude");
            DefaultHapticDuration = ConfigManager.GetFloatConfig("HapticDuration");
            DefaultHapticFrequency = ConfigManager.GetFloatConfig("HapticFrequency");
        }
        
        [Button("Enable Haptics")]
        public void SetHapticsDefine()
        {
#if UNITY_EDITOR
            BuildTargetGroup targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string defineSymbol = "ENABLE_UNITYHAPTICS";
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            if (!defines.Contains(defineSymbol))
            {
                defines += $";{defineSymbol}";
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, defines);
            }
#endif
        }
        
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
        
        public static void ApplyHaptic()
        {
#if (UNITY_ANDROID || UNITY_IOS) && ENABLE_UNITYHAPTICS
            var context = GetContext<MobileToolsManager>();
            if (context == null)
            {
                Debug.LogWarning("Add Mobile tools manager to apply haptic feedback");
                return;
            }

            if (!context.HapticsToggled) return;

            context.PlayHapticEffect();
#endif

        }
        private void PlayHapticEffect()
        {
#if (UNITY_ANDROID || UNITY_IOS) && ENABLE_UNITYHAPTICS
            //Use queue
            HapticQueue ??= new Queue<HapticPlayData>();
            if (Time.time -_lastVibrationTime < VibrationCooldown)
            {
                return;
            }
            
            _lastVibrationTime = Time.time;
            HapticPlayData defaultData = new HapticPlayData()
            {
                Magitude = DefaultHapticMagnitude,
                Duration = DefaultHapticDuration,
                Intensity = DefaultHapticFrequency,
            };
            HapticQueue.Enqueue(defaultData);
            if (!_isHapticsPlaying)
            {
                StartCoroutine(ProcessQueue());
            }
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
            HapticQueue.Enqueue(data);
            if (!_isHapticsPlaying)
            {
                StartCoroutine(ProcessQueue());
            }
        }

        private IEnumerator ProcessQueue()
        {
#if (UNITY_ANDROID || UNITY_IOS) && ENABLE_UNITYHAPTICS

            _isHapticsPlaying = true;
            while (HapticQueue.Count > 0)
            {
                HapticPlayData playData = HapticQueue.Dequeue();
                //HapticPatterns.PlayPreset(HapticPatterns.PresetType.LightImpact);
                HapticPatterns.PlayConstant(playData.Magitude, playData.Intensity, playData.Duration);
                yield return new WaitForSeconds(playData.Duration);
            }
            _isHapticsPlaying = false;
            #else
        yield break;
            
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
        
        [ConsoleMethod("setHapticMagnitude", "Sets haptic feedback magnitude")]
        public static void SetHapticMagnitude(float mag)
        {
            var context = GetContext<MobileToolsManager>();
            if (context == null)
            {
                return;
            }
            context.DefaultHapticMagnitude = mag;
        }
        
        [ConsoleMethod("setHapticDuration", "Sets haptic feedback duration")]
        public static void SetHapticDuration(float dur)
        {
            var context = GetContext<MobileToolsManager>();
            if (context == null)
            {
                return;
            }
            context.DefaultHapticDuration = dur;
        }
        
        [ConsoleMethod("setHapticIntensity", "Sets haptic feedback intensity")]
        public static void SetHapticIntensity(float intensity)
        {
            var context = GetContext<MobileToolsManager>();
            if (context == null)
            {
                return;
            }
            context.DefaultHapticFrequency = intensity;
        }
        #endregion

    }
}