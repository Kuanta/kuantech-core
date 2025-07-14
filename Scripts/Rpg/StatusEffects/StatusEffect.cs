using System;
using Kuantech.Core.FX;
using UnityEngine;

namespace Kuantech.Core.Combat
{
    /// <summary>
    /// Stores parameters for status effect
    /// </summary>
    public class StatusEffectData
    {
        public float TickPeriod;
        public float Duration;
    }
    
    [Serializable]
    public class StatusEffect
    {
        public Actor Target;
        
        public float ApplyTime;
        public float LastTickTime;
        public bool ToBeRemoved;
        
        //Runtime
        public StatusEffectAsset StatusEffectAsset;
        public StatusEffectData ApplyData;
        public int Rank;
        
        [NonSerialized] public Effect StatusFx; //The effect that is played when the status effect is applied

        public virtual void Initialize(StatusEffectAsset asset, StatusEffectData applyData)
        {
            StatusEffectAsset = asset;
            ApplyData = applyData;
        }
        
        public string GetId()
        {
            return StatusEffectAsset.GetId();
        }

        public virtual void SetRank(int rank)
        {
            Rank = rank;
        }
        
        public virtual void OnAdd(Actor targetActor)
        {
            Target = targetActor;
            ApplyTime = Time.time;
            LastTickTime = Time.time;

            if (!StatusEffectAsset.EffectPlayer.IsNull())
            {
                EffectPlaySettings settings = EffectPlaySettings.GetPlayAtObjectSettings(Target.transform, Vector3.zero, Quaternion.identity);
                StatusFx = StatusEffectAsset.EffectPlayer.PlayEffect(settings);
            }
        }
        
        public virtual void OnTick()
        {
            
        }

        #region Getters

        public float GetDuration()
        {
            return ApplyData.Duration;
        }

        public float GetElapsedTime()
        {
            return Time.time - ApplyTime;
        }

        public float GetTickRate()
        {
            return ApplyData.TickPeriod;
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
        #endregion
 

        
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