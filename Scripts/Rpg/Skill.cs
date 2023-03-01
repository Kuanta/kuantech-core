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
        public float Cooldown;
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
                if (Time.time - LastCastTime < SkillData.Cooldown) return false;
                LastCastTime = Time.time;
                return true;
            }

            caster.StartCoroutine(CastRoutine());
            return true;
        }

        private IEnumerator CastRoutine()
        {
            yield return new WaitForSeconds(CastDelay);
            CastAfterTime();
        }

        protected virtual void CastAfterTime()
        {
            
        }
    }
}