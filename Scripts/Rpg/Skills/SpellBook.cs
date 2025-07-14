using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Rpg.Skills
{
    public class SpellBook : ActorModule
    {
        public Skill CurrentlyCastedSkill;
        public float GlobalCooldown;
        private Dictionary<string, Skill> _skills = new Dictionary<string, Skill>();
        
        //Runtime
        private float _lastSkillCastTime;

        public override void ModuleUpdate()
        {
            base.ModuleUpdate();
            if (CurrentlyCastedSkill != null && CurrentlyCastedSkill.IsCasting())
            {
                CurrentlyCastedSkill.UpdateSkill(Time.deltaTime);
            }    
        }
        
        #region Skill Management
        public Skill AddSkill(SkillDataAsset skillAsset)
        {
            if (HasSkill(skillAsset)) return null;
            Skill skill = new Skill();
            skill.Initialize(this, skillAsset);
            _skills[skillAsset.SkillId] = skill;
            return skill;
        }

        public bool HasSkill(SkillDataAsset skilLDataAsset)
        {
            Skill skill = GetSkillByDataAsset(skilLDataAsset);
            if (skill == null) return false;
            return true;
        }

        public Skill GetSkillByDataAsset(SkillDataAsset skillDataAsset)
        {
            return GetSkillById(skillDataAsset.SkillId);
        }

        public Skill GetSkillById(string skillId)
        {
            if (_skills.IsNullOrEmpty() || !_skills.ContainsKey(skillId)) return null;
            return _skills[skillId];
        }
        
        public bool CastSkill(SkillDataAsset skillDataAsset, SkillCastData skillCastData)
        {
            if (!CanCastSkill(skillDataAsset)) return false;
            Skill skillToCast = GetSkillByDataAsset(skillDataAsset);
            if (skillToCast == null) return false;
            if (!skillToCast.CanBeCast(skillCastData)) return false;
            skillToCast.Cast(skillCastData);
            return true;
        }

        public bool CanCastSkill(SkillDataAsset skillDataAsset)
        {
            if (!HasSkill(skillDataAsset)) return false;
            return true;
        }
        #endregion

        #region Events

        public void OnSkillCastStarted(Skill skill)
        {
            CurrentlyCastedSkill = skill;
        }

        public void OnSkillBehaviourStarted(SkillBehaviour skillBehaviour)
        {
            
        }

        public void OnSkillCastEnded(Skill skill)
        {
            CurrentlyCastedSkill = null;
        }
        #endregion
    }
}