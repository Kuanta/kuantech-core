using System;
using Kuantech.Core;
using Kuantech.Core.FX;
using UnityEngine;

namespace Kuantech.Rpg.Skills
{
    [Serializable]
    public class SkillBehaviourType
    {
        [SerializeField] private string className;

        public Type Type => string.IsNullOrEmpty(className) ? null : Type.GetType(className);
        public string ClassName => className;
    }
    
    [Serializable]
    public struct SkillBehaviourData
    {
        public SkillBehaviourType SkillBehaviourType;
        public float Duration;
        
        //Behaviour effect
        public EffectPlayer BehaviourFx;
        
        //Animation data
        public AnimatorParametersData BehaviourStartAnimationData;
    }
    
    public class SkillBehaviour
    {
        //Runtime
        [NonSerialized] public Skill ParentSkill;
        [NonSerialized] public SkillBehaviourData BehaviourData;
        [NonSerialized] public SkillCastData CurrentSkillCastData;
    
        private bool _isCompleted;
        private float _castStartTime;

        #region Lifecycle

        public void Initialize(Skill parentSkill, SkillBehaviourData behaviourData)
        {
            ParentSkill = parentSkill;
            BehaviourData = behaviourData;
        }

        public void StartBehaviour(SkillCastData skillCastData)
        {
            CurrentSkillCastData = skillCastData;
            _castStartTime = Time.time;
            _isCompleted = false;
            
            //Play animation
            
            //Play effect
            
            OnBehaviourStarted();
        }

        protected virtual void OnBehaviourStarted()
        {
            
        }
        
        public void UpdateBehaviour()
        {
            if (_isCompleted) return;
            float duration = GetDuration();
            
            //ıf duration is less than 0, it means the behaviour is infinite
            if (GetElapsedTime() >= duration && duration >= 0)
            {
                CompleteBehaviour();
                return;
            }
            
            //Behaviour
            BehaviourImplementation();
        }

        protected virtual void BehaviourImplementation()
        {
            
        }
        public void CompleteBehaviour()
        {
            _isCompleted = true;
            ParentSkill.OnSkillBehaviourCompleted();
        }
        #endregion

        #region Effects

        public void PlaySkillEffect(EffectPlaySettings playSettings)
        {
            if (BehaviourData.BehaviourFx.IsNull()) return;
            
            //Try to play the effect on actor effect module if possible
            EffectsModule effectModule = ParentSkill.ParentSpellBook.Actor.GetModule<EffectsModule>();
            if (effectModule != null)
            {
               
            }
            else
            {
                //Play at given point
                BehaviourData.BehaviourFx.PlayEffect(playSettings);
            }
        }

        #endregion
        public virtual void ClearBehaviour()
        {
            
        }
        
        #region Queries

        public bool IsCompleted()
        {
            return _isCompleted;
        }

        public float GetElapsedTime()
        {
            return Time.time - _castStartTime;
        }

        public float GetDuration()
        {
            return BehaviourData.Duration;
        }

        #endregion
    }


    
}