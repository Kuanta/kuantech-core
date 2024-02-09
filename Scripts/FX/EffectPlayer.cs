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
        public void PlayEffect()
        {
            PlayEffect(EffectPlaySettings.GetDefaultSettings());
        }
        public void PlayEffect(EffectPlaySettings settings)
        {
            if(Effect != null)
            {
                settings.DespawnAfterPlay = false; //This is probably bound to a gameobject. Don't despawn
                Effect.Play(settings);
                return;
            }
            if(EffectPrefab != null)
            {
                settings.DespawnAfterPlay = true; //Initialized prefabs should be despawned. They won't be despawned if they are bound to effects library so have no fear
                EffectsLibrary.PlayEffect(EffectPrefab.EffectId, settings);
            }else if(!EffectId.IsNullOrEmpty())
            {
                EffectsLibrary.PlayEffect(EffectId, settings);
            }
        }
        public void PlayEffectAtPosition(Vector3 position, Quaternion rotation)
        {
            EffectPlaySettings settings = EffectPlaySettings.GetPlayAtPositionSettings(position, rotation);
            PlayEffect(settings);
        }
    }
}