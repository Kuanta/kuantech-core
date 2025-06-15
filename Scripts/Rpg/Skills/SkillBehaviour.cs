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
        private Effect _playedEffect;

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

        public void PlayEffectAtCastPosition()
        {
            Vector3 effectPos = CurrentSkillCastData.CastPosition;
            Vector3 effectDir = CurrentSkillCastData.CastDirection;
            Quaternion playRot = Quaternion.LookRotation(effectDir);
            EffectPlaySettings playSettings = EffectPlaySettings.GetPlayAtPositionSettings(effectPos, playRot);
            PlaySkillEffect(playSettings);
        }
        
        /// <summary>
        /// A common utility method that plays an effect given id and play settings
        /// </summary>
        /// <param name="effectId"></param>
        /// <param name="playSettings"></param>
        public void PlaySkillEffect(EffectPlaySettings playSettings)
        {
            _playedEffect = null;
            if (BehaviourData.BehaviourFx.IsNull()) return;
            string effectId = BehaviourData.BehaviourFx.GetEffectId();
            
            //Try to play the effect on actor effect module if possible
            EffectsModule effectModule = ParentSkill.ParentSpellBook.Actor.GetModule<EffectsModule>();
            if (effectModule != null)
            {
                EffectPlayer player = effectModule.GetEffectPlayer(effectId);
                if (player != null)
                {
                    _playedEffect = player.PlayEffect(playSettings);
                    return;
                }
            }
            
            //Play at given point
            _playedEffect = BehaviourData.BehaviourFx.PlayEffect(playSettings);
        }

        public void StopSkillEffect()
        {
            if (_playedEffect == null) return;
            _playedEffect.Stop();
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