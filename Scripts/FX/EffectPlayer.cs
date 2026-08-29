using System;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.FX
{  
    /// <summary>
    /// EffectPlayer is a utility tool to play effects using the EffectsLibrary
    /// </summary>
    [Serializable]
    public class EffectPlayer
    {
        public string EffectId;
        public Effect Effect;
        public Effect EffectPrefab;

        public void CopyFrom(EffectPlayer other)
        {
            EffectId = other.EffectId;
            EffectPrefab = other.EffectPrefab;
        }
        public string GetEffectId()
        {
            if (EffectPrefab != null)
            {
                return EffectPrefab.EffectId;
            }

            if (Effect != null)
            {
                return Effect.EffectId;
            }
            return EffectId;
        }
        
        public Effect PlayEffect()
        {
            return PlayEffect(EffectPlaySettings.GetDefaultSettings());
        }
        public Effect PlayEffect(EffectPlaySettings settings)
        {
            if(Effect != null)
            {
                settings.DespawnAfterPlay = false; //This is probably bound to a gameobject. Don't despawn
                Effect.Play(settings);
                return Effect;
            }

            // If the caster already carries this effect on its body (matched by id in its EffectsModule),
            // reuse that one instead of spawning a fresh copy from the library — so the effect plays on the
            // actor and follows it. Mirrors what the tag path does for socketed EffectPlayerComponents.
            if (settings.Caster != null)
            {
                EffectsModule effectsModule = settings.Caster.GetModule<EffectsModule>();
                Effect onActorEffect = effectsModule != null ? effectsModule.GetExistingEffect(GetEffectId()) : null;
                if (onActorEffect != null)
                {
                    settings.DespawnAfterPlay = false; // bound to the actor — don't despawn
                    onActorEffect.Play(settings);
                    return onActorEffect;
                }
            }

            if(EffectPrefab != null)
            {
                settings.DespawnAfterPlay = true; //Initialized prefabs should be despawned. They won't be despawned if they are bound to effects library so have no fear
                return EffectsLibrary.PlayEffectPrefab(EffectPrefab, settings);
            }
            if(!EffectId.IsNullOrEmpty())
            {
                return EffectsLibrary.PlayEffect(EffectId, settings);
            }
            // Nothing configured (Effect/EffectPrefab/EffectId all unset) -- no-op rather than falling back
            // to a tag, which used to default to 0 and silently collide with whatever happened to be
            // registered under tag 0 elsewhere. See EffectsModule.AttackEffect for the bug this caused.
            return null;
        }
        public Effect PlayEffectAtPosition(Vector3 position, Quaternion rotation)
        {
            EffectPlaySettings settings = EffectPlaySettings.GetPlayAtPositionSettings(position, rotation);
            settings.DespawnAfterPlay = true;
            return PlayEffect(settings);
        }
        
        /// <summary>
        /// Checks if the EffectPlayer is null, meaning it has no effect to play.
        /// </summary>
        /// <returns></returns>
        public bool IsNull()
        {
            if(Effect == null && EffectPrefab == null && EffectId.IsNullOrEmpty())
            {
                return true;
            }

            return false;
        }
    }
}