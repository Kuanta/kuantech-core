using System;
using Kuantech.Core.FX;
using UnityEngine;

namespace Kuantech.Core.Combat
{
    [Serializable]
    public class StatusEffectData
    {
        public EffectPlayer EffectPlayer;
        public MetadataAsset MetaData;
        public float Duration;
        public bool Stackable;
        [Tooltip("For non stackable status effects, the existing one will be refreshed")] public bool RefreshOnApply;
    }

    [Serializable]
    public abstract class StatusEffect
    {
        public Actor Target;
        public float TickPeriod;
        
        public float ApplyTime;
        public float LastTickTime;
        public bool ToBeRemoved;
        
        public StatusEffectData StatusEffectData;

        [NonSerialized] public Effect StatusFx; //The effect that is played when the status effect is applied

        public string GetId()
        {
            return StatusEffectData.MetaData.Id;
        }
        
        public virtual void Init(StatusEffectData statusEffectData)
        {
            StatusEffectData = statusEffectData;
        }

        public virtual void OnAdd(Actor targetActor)
        {
            Target = targetActor;
            ApplyTime = Time.time;
            LastTickTime = Time.time;

            if (!StatusEffectData.EffectPlayer.IsNull())
            {
                EffectPlaySettings settings = EffectPlaySettings.GetPlayAtObjectSettings(Target.transform, Vector3.zero, Quaternion.identity);
                StatusFx = StatusEffectData.EffectPlayer.PlayEffect(settings);
            }
        }
        
        public virtual void OnTick()
        {
            
        }

        public float GetDuration()
        {
            if (StatusEffectData == null) return -1;
            return StatusEffectData.Duration;
        }

        public float GetElapsedTime()
        {
            return Time.time - ApplyTime;
        }
        
        /// <summary>
        /// Returns the remaining time
        /// </summary>
        /// <returns></returns>
        public float GetRemainingTime()
        {
            float duration = GetDuration();
            if (duration < 0) return -1;
            float elapsed = GetElapsedTime();
            return Mathf.Max(duration - elapsed, 0.0f);
        }
        
        /// <summary>
        /// Returns normalized elapsed time
        /// </summary>
        /// <returns></returns>
        public float GetNormalizedElapsedTime()
        {
            float duration = GetDuration();
            if (duration < 0) return -1;
            float elapsed = GetElapsedTime();
            return Mathf.Clamp01(elapsed / duration);
        }
        
        /// <summary>
        /// Checks if effect is expired
        /// </summary>
        /// <returns></returns>
        public bool IsExpired()
        {
            float normalized = GetNormalizedElapsedTime();
            if (normalized < 0) return false;
            return normalized >= 1;
        }
        
        public virtual void OnRemove()
        {
            if (StatusFx != null)
            {
                StatusFx.Stop();
            }
        }
        
        /// <summary>
        /// Refreshes the status effect
        /// </summary>
        public virtual void Refresh()
        {
            ApplyTime = Time.time;
        }
    }
}