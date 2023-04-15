using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core.Rpg
{
    
    [Serializable]
    public struct SkillData
    {
        public int Id;
        public string Name;
        public string Description;
        public Sprite Icon;
        public float BaseEnergyCost;
        public bool IsActive;
        public SkillVariable Cooldown;
        public float CastDelay;
        public StatTypes MainStatType;
        public List<SkillVariable> SkillVariables;
    }
    
    public class Skill
    {
        public int Id;
        protected Dictionary<string, SkillVariable> SkillVariables = new Dictionary<string, SkillVariable>();
        protected SkillData SkillData;
        protected float CastDelay;

        protected float LastCastTime = 0f; //For active skills
        public Skill(SkillData data)
        {
            SkillData = data;
            Id = data.Id;
            CastDelay = data.CastDelay;
            if (SkillData.SkillVariables == null) return;
            foreach (var skillVariable in SkillData.SkillVariables)
            {
                SkillVariables[skillVariable.Name] = skillVariable;
            }
        }

        public virtual bool Cast(Actor caster)
        {
            if (!caster.CombatModule.CanCastSkill()) return false;
            if (CastDelay <= 0)
            {
                if (Time.time - LastCastTime < GetCooldown(caster)) return false;
                LastCastTime = Time.time;
                return true;
            }

            caster.StartCoroutine(CastRoutine(caster));
            return true;
        }

        private IEnumerator CastRoutine(Actor caster)
        {
            yield return new WaitForSeconds(CastDelay);
            if (Time.time - LastCastTime < GetCooldown(caster));
            LastCastTime = Time.time;
            CastAfterTime();
        }

        protected virtual void CastAfterTime()
        {
            
        }

        public virtual float GetCooldown(Actor caster)
        {
            //Get cooldown reduction
            float cdReduction = Mathf.Clamp(caster.Stats.GetStat(StatTypes.CooldownReduction), 0, 1);
            float baseValue = SkillData.Cooldown.GetValue();
            float finalValue = Mathf.Max(baseValue * (1 - cdReduction), 0.1f); //Min cooldown should be 0.1
            return finalValue;
        }
    }
}