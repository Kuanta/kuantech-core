using System;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Serialization;

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
        public bool SetRotation;
        public Transform EffectParent;
        public Vector3 LocalPlayPosition;
        public Quaternion LocalPlayRotation;

        //Play at position
        public Vector3 PlayStartPosition;
        public Quaternion PlayStartRotation;
        
        //End position. For beam like effects where an end position is needed
        public WorldPoint PlayEndPoint;
        
        public static EffectPlaySettings GetDefaultSettings()
        {
            return new EffectPlaySettings()
            {
                Duration = -1,
                EffectCooldown = -1,
                DespawnAfterPlay = false,
                SetPosition = false,
                SetRotation = false,
                EffectParent = null,
                LocalPlayPosition = Vector3.zero,
                LocalPlayRotation = Quaternion.identity,
                PlayStartPosition = Vector3.zero,
                PlayStartRotation = Quaternion.identity,
            };
        }

        public static EffectPlaySettings GetPlayAtPositionSettings(Vector3 position, Quaternion rotation)
        {
            EffectPlaySettings settings = GetDefaultSettings();
            settings.PlayStartPosition = position;
            settings.PlayStartRotation = rotation;
            settings.SetPosition = true;
            settings.SetRotation = true;
            return settings;
        }

        public static EffectPlaySettings GetPlayAtObjectSettings(Transform target, Vector3 localPos,
            Quaternion localRotation)
        {
            EffectPlaySettings settings = GetDefaultSettings();
            settings.SetPosition = true;
            settings.SetRotation = true;
            settings.LocalPlayPosition = localPos;
            settings.EffectParent = target;
            settings.LocalPlayRotation = localRotation;
            return settings;
        }
    }
}