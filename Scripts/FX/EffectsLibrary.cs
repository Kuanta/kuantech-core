using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core.FX
{
    public enum EffectTypes
    {
        None,
    }
    
    [Serializable]
    public class EffectsDictionary : SerializableDictionary<int, Effect>{}
    
    public class EffectsLibrary : Singleton<EffectsLibrary>
    {
        public AudioLibrary AudioLibrary;
        public EffectsDictionary EffectsDictionary;
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
        
        public Effect GetEffect(EffectTypes effectType)
        {
            if (EffectsDictionary == null || !EffectsDictionary.ContainsKey((int)effectType)) return null;
            GameObject obj = EffectsPool.GetObject(EffectsDictionary[(int)effectType].gameObject);
            return obj.GetComponent<Effect>();
        }

        public void PlayAudio(AudioTypes audioType)
        {
            AudioLibrary.PlaySound(audioType);
        }
        
        public Effect PlayEffect(EffectTypes effectType, Transform parent, float effectCooldown)
        {
            Effect effect = GetEffect(effectType);
            if (effect == null) return null;
            effect.Play(parent, Vector3.zero, Quaternion.identity, effectCooldown);
            return effect;
        }
        
        public Effect PlayEffect(EffectTypes effectType, Transform parent, Vector3 localPosition, Quaternion localRotation, float effectCooldown)
        {
            Effect effect = GetEffect(effectType);
            if (effect == null) return null;
            effect.Play(parent, localPosition, localRotation, effectCooldown);
            return effect;
        }

        public Effect PlayEffect(EffectTypes effectType, Vector3 localPosition, Quaternion localRotation, float effectCooldown = -1)
        {
            Effect effect = GetEffect(effectType);
            if (effect == null) return null;
            effect.Play(localPosition, localRotation, effectCooldown);
            return effect;
        }
        
        public Effect PlayTimedEffect(EffectTypes effectType, Transform parent, float duration = -1, float effectCooldown = -1)
        {
            Effect effect = GetEffect(effectType);
            if (effect == null) return null;
            effect.PlayTimed(duration >= 0 ? duration : effect.Duration, parent, Vector3.zero, Quaternion.identity, effectCooldown);
            return effect;
        }
        
        public Effect PlayTimedEffect(EffectTypes effectType, Transform parent, Vector3 localPosition, Quaternion localRotation, float duration = -1, float effectCooldown = -1)
        {
            Effect effect = GetEffect(effectType);
            if (effect == null) return null;
            effect.PlayTimed(duration >= 0 ? duration : effect.Duration, parent, localPosition, localRotation, effectCooldown);
            return effect;
        }

        public Effect PlayTimedEffect(EffectTypes effectType, Vector3 position, Quaternion rotation, float duration = -1, float effectCooldown = -1)
        {
            Effect effect = GetEffect(effectType);
            if (effect == null) return null;
            effect.PlayTimed(duration >= 0 ? duration : effect.Duration, position, rotation, effectCooldown);
            return effect;
        }

        public EffectTypes GetEffectType(int i)
        {
            return (EffectTypes) i;
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