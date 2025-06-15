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
        public Effect Effect;
        public Effect EffectPrefab;
        public string EffectId;

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
            if(EffectPrefab != null)
            {
                settings.DespawnAfterPlay = true; //Initialized prefabs should be despawned. They won't be despawned if they are bound to effects library so have no fear
                return EffectsLibrary.PlayEffect(EffectPrefab.EffectId, settings);
            }else if(!EffectId.IsNullOrEmpty())
            {
                return EffectsLibrary.PlayEffect(EffectId, settings);
            }
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