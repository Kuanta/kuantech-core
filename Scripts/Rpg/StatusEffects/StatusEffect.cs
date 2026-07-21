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
    [Serializable]
    public class StatusEffectApplyData
    {
        public Actor Applier;
        public float TickPeriod;
        public float Duration;
    }

    [Serializable]
    public class StatusEffectSerializableData
    {
        public string StatusEffectId;
        public float RemainingDuration; // session-independent, unlike AppliedTime
        public float TimeSinceLastTick;
        public bool ToBeRemoved;
        public int Rank;
        public float Duration;
        public float TickPeriod;
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
        public StatusEffectApplyData ApplyData;
        public int Rank;
        
        [NonSerialized] public Effect StatusFx; //The effect that is played when the status effect is applied

        private Dictionary<string, StatusEffectVariable> _statusEffectVariables;

        public virtual void Initialize(StatusEffectAsset asset, StatusEffectApplyData applyData)
        {
            StatusEffectAsset = asset;
            ApplyData = applyData;
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

        #region Serialization

        public virtual StatusEffectSerializableData BuildState()
        {
            var data = new StatusEffectSerializableData
            {
                StatusEffectId    = GetId(),
                RemainingDuration = GetRemainingTime(),
                TimeSinceLastTick = Time.time - LastTickTime,
                ToBeRemoved       = ToBeRemoved,
                Rank              = Rank,
                Duration          = GetDuration(),
                TickPeriod        = GetTickRate(),
            };
            return data;
        }

        /// <summary>
        /// Restores timing state after reconstruction via CreateStatusEffect + OnAdd.
        /// Call after OnAdd so ApplyTime is set.
        /// </summary>
        public virtual void ApplyState(StatusEffectSerializableData data)
        {
            Rank = data.Rank;
            // Shift ApplyTime so remaining duration matches what was saved
            ApplyTime    = Time.time - (data.Duration - data.RemainingDuration);
            LastTickTime = Time.time - data.TimeSinceLastTick;
            ToBeRemoved  = data.ToBeRemoved;
        }

        #endregion

        #region Status Effect Variables

        /// <summary>
        /// Overrides one variable's value for THIS application only — for callers that compute the number
        /// themselves (a skill passing its rank-scaled damage into a burn it applies).
        ///
        /// The asset's variable data is shared by every application of the effect, so it is copied rather
        /// than written to; any attribute scaling the asset declared is kept.
        /// </summary>
        public void SetVariableOverride(string key, float value)
        {
            if (string.IsNullOrEmpty(key)) return;
            _statusEffectVariables ??= new Dictionary<string, StatusEffectVariable>();

            StatusEffectVariableData data = _statusEffectVariables.TryGetValue(key, out var existing)
                ? existing.StatusEffectVariableData // struct copy — keeps AttributeToScaleWith etc.
                : new StatusEffectVariableData { VariableId = key };
            data.Value = value;

            _statusEffectVariables[key] = new StatusEffectVariable(data) { ParentStatusEffect = this };
        }

        public float GetVariable(string key, float defaultValue = 0)
        {
            if (_statusEffectVariables.IsNullOrEmpty() || !_statusEffectVariables.ContainsKey(key)) return defaultValue;
            StatusEffectVariable variable = _statusEffectVariables[key];
            return variable.GetValue();
        }

        #endregion
    }
}