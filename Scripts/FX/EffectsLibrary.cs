using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.FX
{
    
    public class EffectsLibrary : SubManager
    {
        public AudioLibrary AudioLibrary;
        [Tooltip("Effects that will be created dynamicall")]
        public List<Effect> EffectsPrefabs;
        public Dictionary<int, Effect> _effectsByTag;
        public Dictionary<string, Effect> _effectsById;

        [Tooltip("Parent for existing effects")]
        public Transform ExistingEffectsParent;
        public Dictionary<string, Effect> _existingEffectsById;

        public PrefabPool EffectsPool;
        
        private Dictionary<string, float> _effectLastPlayedTimes = new Dictionary<string, float>();

        public override async UniTask Initialize(GameManager gameManager)
        {
            EffectsPool = new PrefabPool(transform, 1000);

            _effectsById = new Dictionary<string, Effect>();
            foreach(var entry in EffectsPrefabs)
            {
               _effectsById[entry.EffectId] = entry;
            }

            //Existing effects
            if(ExistingEffectsParent == null)
            {
                ExistingEffectsParent = transform;
            }
            Effect[] existingEffects = GetComponentsInChildren<Effect>();
            _existingEffectsById = new Dictionary<string, Effect>();
            foreach (var existingEffect in existingEffects)
            {
                _existingEffectsById[existingEffect.EffectId] = existingEffect;
                existingEffect.BoundToEffectsLibrary =  true;
            }
            if(AudioLibrary != null) AudioLibrary.Initialize();
        }
        
        public static void PlayEffect(string EffectId, EffectPlaySettings settings)
        {
            EffectsLibrary context = GetContext<EffectsLibrary>();
            if(context == null) return;
            Effect effect = context.GetEffectPrefabById(EffectId);
            if(effect == null) return;
            effect.Play(settings);
        }

        public static void PlayEffect(Effect effectPrefab, EffectPlaySettings settings)
        {
            EffectsLibrary context = GetContext<EffectsLibrary>();
            if (context == null) return;
            Effect effect = context.CreateEffectFromPrefab(effectPrefab);
            effect.Play(settings);
        }

        public Effect GetEffectPrefabById(string effectId)
        {
            //Check live effects first
            if(!_existingEffectsById.IsNullOrEmpty() && _existingEffectsById.ContainsKey(effectId))
            {
                return _existingEffectsById[effectId];
            }

            //Check prefabs list
            if(_effectsById.IsNullOrEmpty() || !_effectsById.ContainsKey(effectId)) return null;
            Effect effectPrefab = _effectsById[effectId];
            return CreateEffectFromPrefab(effectPrefab);
        }

        private Effect CreateEffectFromPrefab(Effect effectPrefab)
        {
            if (EffectsPool == null)
            {
                return Instantiate(effectPrefab);
            }
            return EffectsPool.GetObject(effectPrefab.gameObject).GetComponent<Effect>();
        }
        public Effect GetEffectByTag(int effectTag)
        {
            if (_effectsByTag == null || !_effectsByTag.ContainsKey(effectTag)) return null;
            GameObject obj = EffectsPool.GetObject(_effectsByTag[effectTag].gameObject);
            return obj.GetComponent<Effect>();
        }

        public static void PlayAudio(int audioType)
        {
            EffectsLibrary context = GetContext<EffectsLibrary>();
            if(context == null) return;
            if(context.AudioLibrary == null) return;
            context.AudioLibrary.PlaySound(audioType);
        }
        
        public Effect PlayEffect(int effectType, Transform parent, float effectCooldown)
        {
            Effect effect = GetEffectByTag(effectType);
            if (effect == null) return null;
            effect.Play(parent, Vector3.zero, Quaternion.identity, effectCooldown);
            return effect;
        }
        
        public Effect PlayEffect(int effectType, Transform parent, Vector3 localPosition, Quaternion localRotation, float effectCooldown)
        {
            Effect effect = GetEffectByTag(effectType);
            if (effect == null) return null;
            effect.Play(parent, localPosition, localRotation, effectCooldown);
            return effect;
        }

        public Effect PlayEffect(int effectType, Vector3 localPosition, Quaternion localRotation, float effectCooldown = -1)
        {
            Effect effect = GetEffectByTag(effectType);
            if (effect == null) return null;
            effect.Play(localPosition, localRotation, effectCooldown);
            return effect;
        }
        
        public Effect PlayTimedEffect(int effectType, Transform parent, float duration = -1, float effectCooldown = -1)
        {
            Effect effect = GetEffectByTag(effectType);
            if (effect == null) return null;
            effect.PlayTimed(duration >= 0 ? duration : effect.Duration, parent, Vector3.zero, Quaternion.identity, effectCooldown);
            return effect;
        }
        
        public Effect PlayTimedEffect(int effectType, Transform parent, Vector3 localPosition, Quaternion localRotation, float duration = -1, float effectCooldown = -1)
        {
            Effect effect = GetEffectByTag(effectType);
            if (effect == null) return null;
            effect.PlayTimed(duration >= 0 ? duration : effect.Duration, parent, localPosition, localRotation, effectCooldown);
            return effect;
        }

        public Effect PlayTimedEffect(int effectType, Vector3 position, Quaternion rotation, float duration = -1, float effectCooldown = -1)
        {
            Effect effect = GetEffectByTag(effectType);
            if (effect == null) return null;
            effect.PlayTimed(duration >= 0 ? duration : effect.Duration, position, rotation, effectCooldown);
            return effect;
        }

        public static bool CanPlaySound(string effectId, float effectCooldown = -1)
        {
            EffectsLibrary context = EffectsLibrary.GetContext<EffectsLibrary>();
            if (effectCooldown < 0) return true;
            if (!context._effectLastPlayedTimes.ContainsKey(effectId)) return true;
            float lastPlayedTime = context._effectLastPlayedTimes[effectId];
            return Time.time - lastPlayedTime >= effectCooldown;
        }

        public static void SetLastPlayedTime(string effectId)
        {
            EffectsLibrary context = EffectsLibrary.GetContext<EffectsLibrary>();
            if(context == null) return;
            if (context._effectLastPlayedTimes == null)
                context._effectLastPlayedTimes = new Dictionary<string, float>();
            context._effectLastPlayedTimes[effectId] = Time.time;
        }
    }
}