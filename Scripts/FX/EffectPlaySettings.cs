using System;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.FX
{
    [Serializable]
    public struct EffectPlaySettings
    {
        public Actor Caster; //If effect is casted by an actor, this is the caster
        
        public float Duration;
        public float EffectCooldown;
        public bool DespawnAfterPlay;
        
        //Combo
        public int ComboIndex;
        
        //Play under parent
        public bool SetPosition; //If true, the position will be set
        public Transform EffectParent;
        public Vector3 LocalPlayPosition;
        public Quaternion LocalPlayRotation;

        //Play at position
        public Vector3 PlayPosition;
        public Quaternion PlayRotation;
        
        //End position. For beam like effects where an end position is needed
        public WorldPoint EndPoint;
        
        public static EffectPlaySettings GetDefaultSettings()
        {
            return new EffectPlaySettings()
            {
                Duration = -1,
                EffectCooldown = -1,
                DespawnAfterPlay = false,
                SetPosition = false,
                EffectParent = null,
                LocalPlayPosition = Vector3.zero,
                LocalPlayRotation = Quaternion.identity,
                PlayPosition = Vector3.zero,
                PlayRotation = Quaternion.identity,
            };
        }

        public static EffectPlaySettings GetPlayAtPositionSettings(Vector3 position, Quaternion rotation)
        {
            EffectPlaySettings settings = GetDefaultSettings();
            settings.SetPosition = true;
            settings.PlayPosition = position;
            settings.PlayRotation = rotation;
            return settings;
        }

        public static EffectPlaySettings GetPlayAtObjectSettings(Transform target, Vector3 localPos,
            Quaternion localRotation)
        {
            EffectPlaySettings settings = GetDefaultSettings();
            settings.SetPosition = true;
            settings.LocalPlayPosition = localPos;
            settings.EffectParent = target;
            settings.LocalPlayRotation = localRotation;
            return settings;
        }
    }
}