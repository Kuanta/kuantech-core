using System;
using System.Collections.Generic;
using Kuantech.Core.FX;
using Kuantech.Rpg.Skills;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.Combat
{
    /// <summary>
    /// Stores parameters for status effect
    /// </summary>
    public class StatusEffectApplyData
    {
        public Actor Applier;
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
        public StatusEffectApplyData ApplyApplyData;
        public int Rank;
        
        [NonSerialized] public Effect StatusFx; //The effect that is played when the status effect is applied

        private Dictionary<string, StatusEffectVariable> _statusEffectVariables;

        public virtual void Initialize(StatusEffectAsset asset, StatusEffectApplyData applyApplyData)
        {
            StatusEffectAsset = asset;
            ApplyApplyData = applyApplyData;
            _statusEffectVariables = new Dictionary<string, StatusEffectVariable>();
            if (!asset.StatusEffectVariables.IsNullOrEmpty())
            {
                foreach (var variableData in asset.StatusEffectVariables)
                {
                    StatusEffectVariable variable = new StatusEffectVariable(variableData);
                    variable.ParentStatusEffect = this;
                    _statusEffectVariables[variable.StatusEffectVariableData.VariableId] = variable;
                }
            }
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
            return ApplyApplyData.Duration;
        }

        public float GetElapsedTime()
        {
            return Time.time - ApplyTime;
        }

        public float GetTickRate()
        {
            return ApplyApplyData.TickPeriod;
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

        #region Status Effect Variables

        public float GetVariable(string key, float defaultValue = 0)
        {
            if (_statusEffectVariables.IsNullOrEmpty() || _statusEffectVariables.ContainsKey(key)) return defaultValue;
            StatusEffectVariable variable = _statusEffectVariables[key];
            return variable.GetValue();
        }

        #endregion
    }
}