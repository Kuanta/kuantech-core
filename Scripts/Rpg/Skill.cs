using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core.Rpg
{
    /// <summary>
    /// Skill variable that can be skilled by a stat
    /// </summary>
    [Serializable]
    public struct SkillVariable
    {
        public string Name;
        public StatTypes BaseStat;
        public float BaseValue;
        public float StatMultiplier;
        public float RankMultiplier;
        [NonSerialized] public Func<float> RankCalculation;
        public float GetValue(int rank = 0, Actor actor = null)
        {
            float baseValue = 0;
            baseValue = RankCalculation?.Invoke() ?? DefaultRankCalculation(rank);
            if (BaseStat == StatTypes.None) return baseValue;
            return actor.Stats.GetStat(BaseStat) * StatMultiplier + BaseValue;
        }
        public float DefaultRankCalculation(int rank)
        {
            return rank * RankMultiplier + BaseValue;
        }
    }
    
    [Serializable]
    public struct SkillData
    {
        public int Id;
        public string Name;
        public string Description;
        public Sprite Icon;
        public float BaseEnergyCost;
        public bool IsActive;
        public StatTypes MainStatType;
        public List<SkillVariable> SkillVariables;
        
        //Timings
        public float CastTime;//This is the time that the actual effects will take place
        public float AnimationTime; //This is the animation time of the skill. Can initiate a global cooldown for this.
        public SkillVariable Cooldown;
    }
    
    public abstract class Skill
    {
        public int Rank = 1;
        protected Dictionary<string, SkillVariable> SkillVariables = new Dictionary<string, SkillVariable>();
        protected SkillData SkillData;
        protected Actor Caster;
        protected float LastCastTime = 0f; //For active skills
        public Skill(SkillData data)
        {
            SkillData = data;
            if (SkillData.SkillVariables == null) return;
            foreach (var skillVariable in SkillData.SkillVariables)
            {
                SkillVariables[skillVariable.Name] = skillVariable;
            }
        }

        public virtual void AddToActor(CombatModule combatModule)
        {
            Caster = combatModule.Actor;
        }

        public virtual void RemoveFromActor()
        {
            
        }

        public virtual void Cancel()
        {
            
        }

        public bool IsOffCooldown(Actor caster)
        {
            return Time.time - LastCastTime > GetCooldown(caster);
        }
        public virtual bool Cast(Actor caster)
        {
            if (caster.CombatModule == null || !caster.CombatModule.CanCastSkill()) return false;
            if (!IsOffCooldown(caster)) return false;
            Caster = caster;
            InitiateCooldown(caster);
            OnCast(caster);
            if (SkillData.CastTime <= 0)
            {
                ApplySkillEffect();
            }
            caster.StartCoroutine(CastRoutine(caster));
            return true;
        }
        protected virtual void InitiateCooldown(Actor caster)
        {
            LastCastTime = Time.time;
            //Apply global cooldown
            float globalCooldownTime = Mathf.Max(Config.GLOBAL_COOLDOWN_TIME, SkillData.AnimationTime);
            caster.CombatModule.GlobalCooldown.StartCooldown(globalCooldownTime);
        }
        private IEnumerator CastRoutine(Actor caster)
        {
            yield return new WaitForSeconds(SkillData.CastTime);
            if (Time.time - LastCastTime < GetCooldown(caster));
            LastCastTime = Time.time;
            ApplySkillEffect();
        }
        
        /// <summary>
        /// This will be called on Cast. Skills can make preparations with this method.
        /// </summary>
        /// <param name="caster"></param>
        protected virtual void OnCast(Actor caster)
        {
            
        }

        /// <summary>
        /// This is the hearth of skills. A skill will implement this in order to be useful
        /// </summary>
        protected abstract void ApplySkillEffect();

        public virtual float GetCooldown(Actor caster)
        {
            //Get cooldown reduction
            float cdReduction = Mathf.Clamp(caster.Stats.GetStat(StatTypes.CooldownReduction), 0, 1);
            float baseValue = SkillData.Cooldown.GetValue();
            float finalValue = Mathf.Max(baseValue * (1 - cdReduction), 0.1f); //Min cooldown should be 0.1
            return finalValue;
        }
        
        #region SkillVariables

        public float GetSkillAnimationTime()
        {
            return Mathf.Max(SkillData.AnimationTime, 0.1f);
        }
        public SkillVariable GetSkillVariable(string variableName)
        {
            if (SkillVariables.ContainsKey(variableName)) return SkillVariables[variableName];
            return new SkillVariable()
            {
                Name = null,
                BaseStat = StatTypes.None,
                BaseValue = 0,
                StatMultiplier = 0,
                RankMultiplier = 0,
            };
        }
        
        public virtual float GetEnergyCost()
        {
            return SkillData.BaseEnergyCost * Rank;
        }
        
        public void IncreaseRank()
        {
            Rank += 1;
        }
        #endregion
    }
}