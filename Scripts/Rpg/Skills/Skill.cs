using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Rpg.Skills
{
    [Serializable]
    public struct SkillCastData
    {
        public Vector3 CastDirection;
        public Vector3 CastPosition;
        public Actor CastTarget;
    }
    
    public class Skill
    {
        private SkillDataAsset _skillDataAsset;
        private List<SkillBehaviour> _skillBehaviours;
        private Dictionary<string, SkillVariable> _skillVariables;
        
        //Runtime
        [NonSerialized] public SpellBook ParentSpellBook;
        [NonSerialized] public SkillCastData CurrentSkillCastData;
        [NonSerialized] public int CurrentSkilLBehaviourIndex;
        [NonSerialized] public SkillBehaviour CurrentSkillBehaviour;
        private bool _isCasting;
        private float _lastCastTime;
        
        public void Initialize(SpellBook spellBook, SkillDataAsset skillDataAsset)
        {
            _skillDataAsset = skillDataAsset;
            ParentSpellBook = spellBook;

            _skillBehaviours = new List<SkillBehaviour>();
            //Create skill behaviours
            foreach (var skillBehaviourData in skillDataAsset.SkillBehaviours)
            {
                Type t = skillBehaviourData.SkillBehaviourType.Type;
                SkillBehaviour behaviour = (SkillBehaviour) Activator.CreateInstance(t);
                behaviour.Initialize(this, skillBehaviourData);
                _skillBehaviours.Add(behaviour);
            }
            
            //Create skill variables
            foreach(var skillVariableData in skillDataAsset.SkillVariableDatas)
            {
                SkillVariable skillVariable = new SkillVariable(skillVariableData);
                if (_skillVariables == null)
                {
                    _skillVariables = new Dictionary<string, SkillVariable>();
                }
                _skillVariables.Add(skillVariableData.VariableId, skillVariable);
            }

            Reset();
        }

        #region Checks

        public bool IsCasting()
        {
            return _isCasting;
        }
        
        /// <summary>
        /// Check if the skill can be casted.
        /// </summary>
        /// <returns></returns>
        public virtual bool CanBeCast(SkillCastData castData)
        {
            if (_skillDataAsset == null || _isCasting) return false;
            float elapsedTime = Time.time - _lastCastTime;
            
            //todo(skill): Check skill resource here
            return elapsedTime >= _skillDataAsset.SkillCooldown;
        }
        #endregion


        
        #region Lifecycle
        public bool Cast(SkillCastData castData)
        {
            if (!CanBeCast(castData) || ParentSpellBook == null) return false;
            CurrentSkillCastData = castData;
            _isCasting = true;
            CurrentSkilLBehaviourIndex = 0;
            ParentSpellBook.OnSkillCastStarted(this);
            StartSkillBehaviour(CurrentSkilLBehaviourIndex);
            _lastCastTime = Time.time;
            return true;
        }
  
        public void UpdateSkill(float deltaTime)
        {
            if (CurrentSkillBehaviour == null) return;
            CurrentSkillBehaviour.UpdateBehaviour();
        }
        
        public void EndCast()
        {
            ClearCurrentSkillBehaviour();
            Reset();
            ParentSpellBook.OnSkillCastEnded(this);
        }


        #endregion

        #region Skill Behaviour

        public void StartSkillBehaviour(int skillEffectIndex)
        {
            CurrentSkillBehaviour = _skillBehaviours[skillEffectIndex];
            CurrentSkillBehaviour.StartBehaviour(CurrentSkillCastData);
        }

        public void OnSkillBehaviourCompleted()
        {
            SetNextSkillBehaviour();
        }
        
        public void SetNextSkillBehaviour()
        {
            ClearCurrentSkillBehaviour();
            CurrentSkilLBehaviourIndex++;
            if (CurrentSkilLBehaviourIndex >= _skillBehaviours.Count)
            {
                EndCast();
                return;
            }
            StartSkillBehaviour(CurrentSkilLBehaviourIndex);
        }
        public void ClearCurrentSkillBehaviour()
        {
            if (CurrentSkillBehaviour == null) return;
            CurrentSkillBehaviour.ClearBehaviour();
            CurrentSkillBehaviour = null;
        }

        #endregion

  
        #region SkillVariables
        
        public SkillVariable GetSkillVariable(string variableId)
        {
            if (_skillVariables.IsNullOrEmpty() || !_skillVariables.ContainsKey(variableId))
            {
                return null;
            }
            return _skillVariables[variableId];
        }
        
        /// <summary>
        /// Returns the current value of the skill
        /// </summary>
        /// <param name="variableId"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public float GetSkillVariableValue(string variableId, float defaultValue = 0)
        {
            SkillVariable variable = GetSkillVariable(variableId);
            if (variable == null) return defaultValue;
            return variable.GetValue();
        }
        #endregion


        public void Reset()
        {
            _isCasting = false;
            CurrentSkilLBehaviourIndex = 0;
        }
    }
}