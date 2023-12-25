using System;
using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.FX
{
    [Serializable]
   public struct EffectEntry
   {
        [KTTag("EffectTag")]
        public int EffectId;
        public Effect Effect;
   }
    
    public class EffectsLibrary : Singleton<EffectsLibrary>
    {
        public AudioLibrary AudioLibrary;
        public Dictionary<int, Effect> _effects;
        public PrefabPool EffectsPool;
        
        private Dictionary<string, float> _effectLastPlayedTimes = new Dictionary<string, float>();
        
        private void Awake()
        {
            EffectsPool = new PrefabPool(transform, 1000);
        }

        public void Initialize()
        {
            if(AudioLibrary != null) AudioLibrary.Initialize();
        }
        
        public Effect GetEffect(int effectType)
        {
            if (_effects == null || !_effects.ContainsKey(effectType)) return null;
            GameObject obj = EffectsPool.GetObject(_effects[effectType].gameObject);
            return obj.GetComponent<Effect>();
        }

        public void PlayAudio(int audioType)
        {
            AudioLibrary.PlaySound(audioType);
        }
        
        public Effect PlayEffect(int effectType, Transform parent, float effectCooldown)
        {
            Effect effect = GetEffect(effectType);
            if (effect == null) return null;
            effect.Play(parent, Vector3.zero, Quaternion.identity, effectCooldown);
            return effect;
        }
        
        public Effect PlayEffect(int effectType, Transform parent, Vector3 localPosition, Quaternion localRotation, float effectCooldown)
        {
            Effect effect = GetEffect(effectType);
            if (effect == null) return null;
            effect.Play(parent, localPosition, localRotation, effectCooldown);
            return effect;
        }

        public Effect PlayEffect(int effectType, Vector3 localPosition, Quaternion localRotation, float effectCooldown = -1)
        {
            Effect effect = GetEffect(effectType);
            if (effect == null) return null;
            effect.Play(localPosition, localRotation, effectCooldown);
            return effect;
        }
        
        public Effect PlayTimedEffect(int effectType, Transform parent, float duration = -1, float effectCooldown = -1)
        {
            Effect effect = GetEffect(effectType);
            if (effect == null) return null;
            effect.PlayTimed(duration >= 0 ? duration : effect.Duration, parent, Vector3.zero, Quaternion.identity, effectCooldown);
            return effect;
        }
        
        public Effect PlayTimedEffect(int effectType, Transform parent, Vector3 localPosition, Quaternion localRotation, float duration = -1, float effectCooldown = -1)
        {
            Effect effect = GetEffect(effectType);
            if (effect == null) return null;
            effect.PlayTimed(duration >= 0 ? duration : effect.Duration, parent, localPosition, localRotation, effectCooldown);
            return effect;
        }

        public Effect PlayTimedEffect(int effectType, Vector3 position, Quaternion rotation, float duration = -1, float effectCooldown = -1)
        {
            Effect effect = GetEffect(effectType);
            if (effect == null) return null;
            effect.PlayTimed(duration >= 0 ? duration : effect.Duration, position, rotation, effectCooldown);
            return effect;
        }

        public static bool CanPlaySound(string effectId, float effectCooldown = -1)
        {
            EffectsLibrary context = EffectsLibrary.Instance;
            if (effectCooldown < 0) return true;
            if (!context._effectLastPlayedTimes.ContainsKey(effectId)) return true;
            float lastPlayedTime = context._effectLastPlayedTimes[effectId];
            return Time.time - lastPlayedTime >= effectCooldown;
        }

        public static void SetLastPlayedTime(string effectId)
        {
            EffectsLibrary context = EffectsLibrary.Instance;
            if (context._effectLastPlayedTimes == null)
                context._effectLastPlayedTimes = new Dictionary<string, float>();
            context._effectLastPlayedTimes[effectId] = Time.time;
        }
    }
}