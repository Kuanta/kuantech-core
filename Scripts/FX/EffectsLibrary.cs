using System;
using UnityEngine;

namespace Kuantech.Core.FX
{
    public enum EffectTypes
    {
        SwordSwing_1,
        SwordSwing_2,
        SwordSwing_3,
        ArrowShoot,
        StaffSwing_1,
        StaffSwing_2,
        LevelUp,
        Slam,
        QuickStrikes,
        MagicOrb,
        StaffShoot,
        GhostWeapon,
        ArcaneArrowExplosion,
    }
    
    [Serializable]
    public class EffectsDictionary : SerializableDictionary<EffectTypes, Effect>{}
    
    public class EffectsLibrary : Singleton<EffectsLibrary>
    {
        public AudioLibrary AudioLibrary;
        public EffectsDictionary EffectsDictionary;
        public PrefabPool EffectsPool;

        private void Awake()
        {
            EffectsPool = new PrefabPool(transform, 1000);
        }

        public Effect GetEffect(EffectTypes effectType)
        {
            if (EffectsDictionary == null || !EffectsDictionary.ContainsKey(effectType)) return null;
            GameObject obj = EffectsPool.GetObject(EffectsDictionary[effectType].gameObject);
            return obj.GetComponent<Effect>();
        }

        public void PlayAudio(AudioTypes audioType)
        {
            AudioLibrary.PlaySound(audioType);
        }
        
        public Effect PlayTimedEffect(EffectTypes effectType, Transform parent)
        {
            Effect effect = GetEffect(effectType);
            if (effect == null) return null;
            effect.PlayTimed(effect.Duration, parent, Vector3.zero, Quaternion.identity);
            return effect;
        }
        
        public Effect PlayTimedEffect(EffectTypes effectType, Transform parent, Vector3 localPosition, Quaternion localRotation)
        {
            Effect effect = GetEffect(effectType);
            if (effect == null) return null;
            effect.PlayTimed(effect.Duration, parent, localPosition, localRotation);
            return effect;
        }

        public Effect PlayTimedEffect(EffectTypes effectType, Vector3 localPosition, Quaternion localRotation)
        {
            Effect effect = GetEffect(effectType);
            if (effect == null) return null;
            effect.PlayTimed(effect.Duration, localPosition, localRotation);
            return effect;
        }
    }
}