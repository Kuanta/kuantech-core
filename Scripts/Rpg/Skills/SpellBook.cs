using System.Collections.Generic;
using System.Linq;
using Kuantech.Core;
using Kuantech.Core.Combat;
using Kuantech.Core.Utils;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Rpg.Skills
{
    public class SpellBook : ActorModule
    {
        //public Skill CurrentlyCastedSkill;
        public float GlobalCooldown;
        private Dictionary<string, Skill> _skills = new Dictionary<string, Skill>();
        private List<Skill> _activeSkills = new(); 
        
        public LockVariable SkillLock = new LockVariable();

        //Runtime
        private float _lastSkillCastTime;
        private HealthcareModule _healthcareModule;

        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            _healthcareModule = Actor.GetModule<HealthcareModule>();
        }

        public override void ModuleUpdate()
        {
            base.ModuleUpdate();
            if (!Actor.IsAlive()) return;

            for (int i = _activeSkills.Count - 1; i >= 0; i--)
            {
                Skill skill = _activeSkills[i];
                if (skill.IsCasting())
                {
                    skill.UpdateSkill(Time.deltaTime);
                }
                else
                {
                    _activeSkills.RemoveAt(i);
                }
            }
        }

        
        #region Skill Management

        public Skill[] GetSkills()
        {
            return _skills.Values.ToArray();
        }
        
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

        public bool HasSkill(string skillId)
        {
            Skill skill = GetSkillById(skillId);
            return skill != null;
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

        public bool CastSkill(Skill skillToCast, ActionCastData skillCastData)
        {
            if (skillToCast == null || skillToCast.SkillDataAsset == null) return false;
            return CastSkill(skillToCast.SkillDataAsset, skillCastData);
        }
        
        public bool CastSkill(SkillDataAsset skillDataAsset, ActionCastData skillCastData)
        {
            if (SkillLock.IsLocked()) return false;
            if (!CanCastSkill(skillDataAsset, skillCastData)) return false;
            Skill skillToCast = GetSkillByDataAsset(skillDataAsset);
            if (skillToCast == null) return false;
            if (!_activeSkills.Contains(skillToCast))
            {
                _activeSkills.Add(skillToCast);
            }
            
            //Spend resource
            if (_healthcareModule != null && skillDataAsset.RequiredResource != null)
            {
                _healthcareModule.RemoveResource(skillDataAsset.RequiredResource, skillDataAsset.RequiredResourceAmount);
            }
            
            //Turn towards skill direction?
            if (skillCastData.Target != null)
            {
                Actor.MotionVectorsHandler.SetTargetObject(skillCastData.Target.transform);
            }
            else
            {
                Actor.MotionVectorsHandler.SetTargetVector(skillCastData.Direction);
            }
            return skillToCast.Cast(skillCastData);
        }

        public bool CanCastSkill(SkillDataAsset skillDataAsset, ActionCastData skillCastData)
        {
            if (!CanSkillBeCasted(skillDataAsset)) return false;
            Skill skill = GetSkillByDataAsset(skillDataAsset);
            return skill.CanBeCast(skillCastData);
        }
        
        /// <summary>
        /// Checks cooldown, skill availability and resource availability
        /// </summary>
        /// <param name="skillDataAsset"></param>
        /// <returns></returns>
        public bool CanSkillBeCasted(SkillDataAsset skillDataAsset)
        {
            if (!HasSkill(skillDataAsset)) return false;
            
            //Check resource
            if (_healthcareModule != null && skillDataAsset.RequiredResource != null)
            {
                if(_healthcareModule.GetCurrentResource(skillDataAsset.RequiredResource) < skillDataAsset.RequiredResourceAmount) return false;
            }

            return true;
        }

        public override void Cleanup()
        {
            base.Cleanup();
            _skills.Clear();
        }
        
        #endregion

        #region Events

        public virtual void OnSkillCastStarted(Skill skill)
        {
            //CurrentlyCastedSkill = skill;
        }

        public void OnSkillBehaviourStarted(SkillBehaviour skillBehaviour)
        {
            
        }

        public virtual void OnSkillCastEnded(Skill skill)
        {
            //CurrentlyCastedSkill = null;
        }
        #endregion
    }
}