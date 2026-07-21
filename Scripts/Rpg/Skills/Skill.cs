using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Rpg.Skills
{
    public class Skill
    {
        public SkillDataAsset SkillDataAsset;
        private List<SkillBehaviour> _skillBehaviours;
        private Dictionary<string, SkillVariable> _skillVariables;
        
        //Runtime
        [NonSerialized] public int SkillRank;
        [NonSerialized] public SpellBook ParentSpellBook;
        [NonSerialized] public ActionCastData CurrentSkillCastData;
        [NonSerialized] public int CurrentSkilLBehaviourIndex;
        [NonSerialized] public SkillBehaviour CurrentSkillBehaviour;
        private bool _isCasting;
        private float _lastCastTime;
        
        public void Initialize(SpellBook spellBook, SkillDataAsset skillDataAsset)
        {
            SkillDataAsset = skillDataAsset;
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
                skillVariable.ParentSkill = this;
                if (_skillVariables == null)
                {
                    _skillVariables = new Dictionary<string, SkillVariable>();
                }
                _skillVariables.Add(skillVariableData.VariableId, skillVariable);
            }

            Reset();

            // Stagger initial cooldown so enemies spawned together don't all cast on the same frame.
            if (skillDataAsset.CooldownJitter > 0f)
                _lastCastTime = Time.time - skillDataAsset.SkillCooldown + UnityEngine.Random.Range(0f, skillDataAsset.CooldownJitter);
        }

        public string GetId()
        {
            if(SkillDataAsset == null) return "";
            return SkillDataAsset.SkillId;
        }

        public void SetSkillRank(int rank)
        {
            SkillRank = rank;
        }

        #region Checks

        public bool IsCasting()
        {
            return _isCasting;
        }

        /// <summary>
        /// Called on observer clients to mark the skill as actively casting
        /// so ModuleUpdate keeps it in _activeSkills and UpdateBehaviour runs.
        /// </summary>
        public void BeginObserverCast()
        {
            _isCasting = true;
        }
        
        public bool IsSkillReady()
        {
            if (SkillDataAsset == null || _isCasting) return false;
            float elapsedTime = Time.time - _lastCastTime;

            if (elapsedTime < SkillDataAsset.SkillCooldown) return false;

            //todo: Check skill resource
            return true;
        }
        /// <summary>
        /// Check if the skill can be casted.
        /// </summary>
        /// <returns></returns>
        public virtual bool CanBeCast(ActionCastData castData)
        {
            if(!IsSkillReady()) return false;
            //Check cast data
            if (!CheckCastData(castData)) return false;
            
            if (SkillDataAsset.SkillCastChecker != null &&
                !SkillDataAsset.SkillCastChecker.CanBeCast(this, castData)) return false;

            return true;
        }
        
        /// <summary>
        /// Checks cast data by skill cast type
        /// </summary>
        /// <returns></returns>
        protected virtual bool CheckCastData(ActionCastData castData)
        {
            switch (SkillDataAsset.SkillCastType)
            {
                case SkillDataAsset.SkillCastTypes.Self:
                    return castData.Target != null && castData.Target == ParentSpellBook.Actor;
                case Skills.SkillDataAsset.SkillCastTypes.Targeted:
                    if (castData.Target == null) return false;
                    break;
                case Skills.SkillDataAsset.SkillCastTypes.Directional:
                    return castData.Direction.sqrMagnitude > 0;
    
            }

            float range = GetRange();
            if (range > 0)
            {
                Vector3 targetPos = castData.Target != null
                    ? castData.Target.transform.position
                    : castData.TargetPosition;
                float sqrDist = Vector3.SqrMagnitude(targetPos - ParentSpellBook.Actor.GetActorLocation());
                if (sqrDist > range * range) return false;
            }

            return true;
        }

        public float GetRange()
        {
            return SkillDataAsset.SkillRange;
        }

        public float GetLastCastTime()
        {
            return _lastCastTime;
        }
        
        #endregion

        #region Lifecycle

        private bool _requireAlignment = false;
        private bool _hasAligned = false;
        public bool Cast(ActionCastData castData)
        {
            if (!CanBeCast(castData) || ParentSpellBook == null) return false;
            _lastCastTime = Time.time;

            CurrentSkillCastData = castData;
            _isCasting = true;
            CurrentSkilLBehaviourIndex = 0;
            ParentSpellBook.OnSkillCastStarted(this);

            _requireAlignment = SkillDataAsset.WaitRotationalAlignToTarget;
            if (_requireAlignment)
            {
                _hasAligned = false;
                return true;
            }

            StartSkillBehaviour(CurrentSkilLBehaviourIndex);
            return true;
        }

        public bool HasAlignedWithTargetDirection()
        {
            Vector3 targetVector = ParentSpellBook.Actor.MotionVectorsHandler.GetTargetVector();
            Vector3 currentVector = ParentSpellBook.Actor.transform.forward;
            
            //Check alignment
            return Vector3.Dot(targetVector, currentVector) >= 0.9f;
        }
        
        public void UpdateSkill(float deltaTime)
        {
            if (_requireAlignment && !_hasAligned)
            {
                if (HasAlignedWithTargetDirection())
                {
                    StartSkillBehaviour(CurrentSkilLBehaviourIndex);
                    _hasAligned = true;
                    return;
                }
            }
            
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
            ParentSpellBook.OnSkillBehaviourStarted(CurrentSkillBehaviour);
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
            return variable.GetValueByRank(SkillRank);
        }
        #endregion

        public void Reset()
        {
            _isCasting = false;
            CurrentSkilLBehaviourIndex = 0;
        }
    }
}