using System;
using System.Collections.Generic;
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
    public struct SkillBehaviorFxData
    {
        public enum SKillBehaviourFxPlayType
        {
            OnCaster, //Attached to caster
            AtCaster, //At casters position, without attaching to caster
            AtCastPoint, //At point of cast

        }

        public SKillBehaviourFxPlayType PlayType;
        public EffectPlayer EffectPlayer;
        public bool StopOnBehaviourEnd;
    }
    
    [Serializable]
    public struct SkillBehaviourData
    {
        public SkillBehaviourType SkillBehaviourType;
        public float Duration;

        [Header("Effects")] 
        public List<SkillBehaviorFxData> SkillBehaviourFxDatas;
        
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
        public HashSet<Effect> PlayedEffects = new HashSet<Effect>();

        #region Lifecycle

        public void Initialize(Skill parentSkill, SkillBehaviourData behaviourData)
        {
            ParentSkill = parentSkill;
            BehaviourData = behaviourData;
        }

        public void StartBehaviour(SkillCastData skillCastData)
        {
            PlayedEffects.Clear();
            CurrentSkillCastData = skillCastData;
            _castStartTime = Time.time;
            _isCompleted = false;
            
            //Play animation
            PlayBehaviourAnimation();
            
            //Play effects
            PlayBehaviourEffects();
            
            OnBehaviourStarted();
        }

        protected virtual void PlayBehaviourAnimation()
        {
            AnimationModule am = ParentSkill.ParentSpellBook.Actor.GetModule<AnimationModule>();
            if (am != null)
            {
                BehaviourData.BehaviourStartAnimationData.SetParameters(am.Animator);
            }
        }

        protected virtual void PlayBehaviourEffects()
        {
            foreach (var fx in BehaviourData.SkillBehaviourFxDatas)
            {
                if(fx.EffectPlayer.IsNull()) continue;
                Effect effect = null;
                switch(fx.PlayType)
                {
                    case SkillBehaviorFxData.SKillBehaviourFxPlayType.OnCaster:
                        effect = PlayEffectOnCaster(fx.EffectPlayer);
                        break;
                    case SkillBehaviorFxData.SKillBehaviourFxPlayType.AtCaster:
                        effect  = PlayEffectAtCasterPosition(fx.EffectPlayer);
                        break;
                    case SkillBehaviorFxData.SKillBehaviourFxPlayType.AtCastPoint:
                        effect = PlayEffectAtCastPosition(fx.EffectPlayer);
                        break;
                }

                if (effect != null && fx.StopOnBehaviourEnd)
                {
                    PlayedEffects.Add(effect);
                }
            }
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

        public Effect PlayEffectAtCastPosition(EffectPlayer effectPlayer)
        {
            if (effectPlayer.IsNull()) return null;
            Vector3 effectPos = CurrentSkillCastData.CastPosition;
            Vector3 effectDir = CurrentSkillCastData.CastDirection;
            Quaternion playRot = Quaternion.identity;
            if (effectDir.sqrMagnitude >= 0.001f)
            {
                playRot = Quaternion.LookRotation(effectDir);
                
            }
            EffectPlaySettings playSettings = EffectPlaySettings.GetPlayAtPositionSettings(effectPos, playRot);
            return effectPlayer.PlayEffect(playSettings);
        }
        
        /// <summary>
        /// Plays effect at caster position
        /// </summary>
        /// <param name="effectPlayer"></param>
        /// <returns></returns>
        public Effect PlayEffectAtCasterPosition(EffectPlayer effectPlayer)
        {
            if (effectPlayer.IsNull()) return null;
            Vector3 effectPos = ParentSkill.ParentSpellBook.Actor.transform.position;
            Quaternion playRot = ParentSkill.ParentSpellBook.Actor.transform.rotation;
            EffectPlaySettings playSettings = EffectPlaySettings.GetPlayAtPositionSettings(effectPos, playRot);
            return effectPlayer.PlayEffect(playSettings);
        }

        public Effect PlayEffectOnCaster(EffectPlayer effectPlayer)
        {
            if (effectPlayer.IsNull()) return null;
            Vector3 effectPos = CurrentSkillCastData.CastPosition;
            Vector3 effectDir = CurrentSkillCastData.CastDirection;
            Quaternion playRot = Quaternion.identity;
            if (effectDir.sqrMagnitude >= 0.001f)
            {
                playRot = Quaternion.LookRotation(effectDir);
            }
            EffectPlaySettings playSettings = EffectPlaySettings.GetPlayAtPositionSettings(effectPos, playRot);

            //Try to play the effect on actor effect module if possible
            EffectsModule effectModule = ParentSkill.ParentSpellBook.Actor.GetModule<EffectsModule>();
            Effect effect;
            if (effectModule != null)
            {
                effect = effectModule.PlayEffectOnActor(effectPlayer);
                if (effect == null)
                {
                    return null;
                }
            }
            effect = effectPlayer.PlayEffect(playSettings);
            return effect;
        }
        
        public void StopSkillEffects()
        {
            foreach (var effect in PlayedEffects)
            {
                effect.Stop(); 
            }
        }
        #endregion
        
        public virtual void ClearBehaviour()
        {
            //clear effects
            StopSkillEffects();
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