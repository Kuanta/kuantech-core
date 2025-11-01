using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.FX;
using Kuantech.Utils;
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
    public struct FxPlayData
    {
        public enum SkillBehaviourFxPlayType
        {
            OnCaster, //Attached to caster
            AtCaster, //At casters position, without attaching to caster
            AtCastPoint, //At point of cast
            OnTarget, //On top of the target
        }

        public SkillBehaviourFxPlayType PlayType;
        public EffectPlayer EffectPlayer;
        public bool StopOnBehaviourEnd;
        [Tooltip("Useful for OnCaster or AtCaster. If play type is OnTarget, effect will be played attached to given slot")]
        public string ActorSlotName;
        [Tooltip("If set to true, will face the effect towards the direction")]
        public bool RotateTowardsDirection;
    }
    
    [Serializable]
    public struct SkillBehaviourData
    {
        public SkillBehaviourType SkillBehaviourType;

        [Header("Config Data")] 
        [Tooltip("Behaviour specific config data")]
        [SerializeReference]
        [SubclassSelector]
        public SkillBehaviourConfigData ConfigData;

        [Header("Common Properties")] 
        public float CastTime;
        public float CastAnimationDuration;
        public float Duration;
        public float EffectPlayTime;
        [Tooltip("If set to true, skill will wait for rotation alignment before starting the behaviour")]
        public float WaitForRotationAlign;

        [Header("Effects")] 
        public List<FxPlayData> SkillBehaviourFxDatas;
        
        //Animation data
        public AnimationData BehaviourStartAnimationData;
        
        [Tooltip("If any animation parameters needed to be cleared('Like clearing a channeled boolean') do it with these")]
        public AnimationData AnimationParametersToClear;
    }
    
    public class SkillBehaviour
    {
        //Runtime
        [NonSerialized] public Skill ParentSkill;
        [NonSerialized] public SkillBehaviourData BehaviourData;
        [NonSerialized] public ActionCastData CurrentSkillCastData;
    
        protected bool _isCompleted;
        private float _castStartTime;
        protected bool _playedEffect;
        public HashSet<Effect> PlayedEffects = new HashSet<Effect>();
        
        /// <summary>
        /// Returns the parent actor
        /// </summary>
        /// <returns></returns>
        public Actor GetParentActor()
        {
            if (ParentSkill == null) return null;
            if (ParentSkill.ParentSpellBook == null) return null;
            return ParentSkill.ParentSpellBook.Actor;
        }
        
        #region Lifecycle

        public virtual void Initialize(Skill parentSkill, SkillBehaviourData behaviourData)
        {
            ParentSkill = parentSkill;
            BehaviourData = behaviourData;
        }

        public virtual void StartBehaviour(ActionCastData skillCastData)
        {
            PlayedEffects.Clear();
            CurrentSkillCastData = skillCastData;
            _castStartTime = Time.time;
            _isCompleted = false;
            _playedEffect = false;
            
            //Play animation
            PlayBehaviourAnimation();

            OnBehaviourStarted();
        }

        protected virtual void PlayBehaviourAnimation()
        {
            AnimationModule am = ParentSkill.ParentSpellBook.Actor.GetModule<AnimationModule>();
            if (am != null)
            {
                am.PlayAnimationData(BehaviourData.BehaviourStartAnimationData, BehaviourData.CastAnimationDuration); //todo: 
            }
        }

        protected virtual void ClearAnimationParameters()
        {
            AnimationModule am = ParentSkill.ParentSpellBook.Actor.GetModule<AnimationModule>();
            if (am != null)
            {
                BehaviourData.AnimationParametersToClear.SetParameters(am.GetAnimator());
            }
        }
        
        protected virtual void PlayBehaviourEffects()
        {
            foreach (var fx in BehaviourData.SkillBehaviourFxDatas)
            {
                if(fx.EffectPlayer.IsNull()) continue;
                Effect effect = null;
                
                //Can effet be played at slot
                Actor playerActor = ParentSkill.ParentSpellBook.Actor;
                ActorSlotsHandler slotsHandler = playerActor.GetModule<ActorSlotsHandler>();

                switch(fx.PlayType)
                {
                    case FxPlayData.SkillBehaviourFxPlayType.OnCaster:
                        if (slotsHandler != null)
                        {
                            Transform slot = slotsHandler.GetSlot(fx.ActorSlotName);
                            if (slot != null)
                            {
                                effect = PlayEffectAtActorSlot(slot, fx.EffectPlayer);
                                break;
                            }
                        }
                        effect = PlayEffectOnCaster(fx.EffectPlayer);

                        break;
                    case FxPlayData.SkillBehaviourFxPlayType.AtCaster:
                        if (slotsHandler != null)
                        {
                            Transform slot = slotsHandler.GetSlot(fx.ActorSlotName);
                            if (slot != null)
                            {
                                effect = PlayEffectAtActorSlotLocation(slot, fx.EffectPlayer);
                                break;
                            }
                        }
                        effect  = PlayEffectAtCasterPosition(fx.EffectPlayer);

                        break;
                    case FxPlayData.SkillBehaviourFxPlayType.OnTarget:
                        if (CurrentSkillCastData.Target != null)
                        {
                            ActorSlotsHandler targetSlotsHandler = CurrentSkillCastData.Target.GetModule<ActorSlotsHandler>();
                            if (targetSlotsHandler != null)
                            {
                                Transform slot = targetSlotsHandler.GetSlot(fx.ActorSlotName);
                                if (slot != null)
                                {
                                    effect = PlayEffectAtActorSlot(slot, fx.EffectPlayer);
                                    break;
                                }
                            }
                        }
             
                        effect = PlayEffectAtTarget(fx.EffectPlayer);
                        break;
                    case FxPlayData.SkillBehaviourFxPlayType.AtCastPoint:
                        effect = PlayEffectAtCastPosition(fx.EffectPlayer);
                        break;
                }

                if (effect != null && fx.RotateTowardsDirection)
                {
                    effect.transform.forward = CurrentSkillCastData.Direction;
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
            float elapsedTime = GetElapsedTime();
                  
            //Behaviour
            if (elapsedTime >= BehaviourData.CastTime)
            {
                BehaviourImplementation();
            }
            
            if(!_playedEffect && GetElapsedTime() > BehaviourData.EffectPlayTime)
            {
                PlayBehaviourEffects();
                _playedEffect = true;
            }
            
            //ıf duration is less than 0, it means the behaviour is infinite
            if (GetElapsedTime() >= duration && duration >= 0)
            {
                CompleteBehaviour();
                return;
            }
        }

        protected virtual void BehaviourImplementation()
        {
            
        }
        
        public void CompleteBehaviour()
        {
            _isCompleted = true;
            OnBehaviourEnded();
            ClearAnimationParameters();
            ParentSkill.OnSkillBehaviourCompleted();
        }

        protected virtual void OnBehaviourEnded()
        {
            
        }
        
        #endregion

        #region Effects

        protected EffectPlaySettings GetEffectPlaySettings(FxPlayData.SkillBehaviourFxPlayType fxPlayType)
        {
            Actor caster = ParentSkill.ParentSpellBook.Actor;
            EffectPlaySettings playSettings = EffectPlaySettings.GetDefaultSettings();
            playSettings.Caster = caster;
            playSettings.PlayEndPoint = GetSkillCastPoint();
            
            Vector3 effectStarPosition = CurrentSkillCastData.StartPosition;
            Vector3 effectStartDir = CurrentSkillCastData.Direction;
            
            Quaternion playRot = Quaternion.identity;
            if (effectStartDir.sqrMagnitude >= 0.001f)
            {
                playRot = Quaternion.LookRotation(effectStartDir);
            }
            Quaternion localRot =
                Quaternion.Inverse(ParentSkill.ParentSpellBook.Actor.transform.rotation) * playRot;
            
            switch(fxPlayType)
            {
                case FxPlayData.SkillBehaviourFxPlayType.OnCaster:
                    playSettings.EffectParent = caster.transform;
                    playSettings.LocalPlayPosition = effectStarPosition - caster.transform.position;
                    playSettings.LocalPlayRotation = localRot;
                    playSettings.SetPosition = true;
                    break;
                case FxPlayData.SkillBehaviourFxPlayType.AtCaster:
                    playSettings.SetPosition = true;
                    playSettings.PlayStartPosition = caster.transform.position;
                    playSettings.PlayStartRotation = playRot;
                    break;
                case FxPlayData.SkillBehaviourFxPlayType.AtCastPoint:
                    playSettings.SetPosition = true;
                    playSettings.PlayStartPosition = effectStarPosition;
                    playSettings.PlayStartRotation = playRot;
                    break;
            }

            return playSettings;
        }

        private WorldPoint GetSkillCastPoint()
        {
            WorldPoint castPoint = new WorldPoint
            {
                Position = CurrentSkillCastData.TargetPosition,
                Rotation = Quaternion.LookRotation(CurrentSkillCastData.Direction),
                Target = CurrentSkillCastData.Target != null ? CurrentSkillCastData.Target.transform : null,
            };

            return castPoint;
        }
        public Effect PlayEffectAtCastPosition(EffectPlayer effectPlayer)
        {
            if (effectPlayer.IsNull()) return null;
            Vector3 effectPos = CurrentSkillCastData.TargetPosition;
            Vector3 effectDir = CurrentSkillCastData.Direction;
            Quaternion playRot = Quaternion.identity;
            if (effectDir.sqrMagnitude >= 0.001f)
            {
                playRot = Quaternion.LookRotation(effectDir);
                
            }
            EffectPlaySettings playSettings = EffectPlaySettings.GetPlayAtPositionSettings(effectPos, playRot);
            playSettings.Caster = ParentSkill.ParentSpellBook.Actor;
            return effectPlayer.PlayEffect(playSettings);
        }

        public Effect PlayEffectAtTarget(EffectPlayer effectPlayer)
        {
            if (effectPlayer.IsNull()) return null;
            Transform target = CurrentSkillCastData.Target != null 
                ? CurrentSkillCastData.Target.transform 
                : null;
            if (target == null) return PlayEffectAtCastPosition(effectPlayer);
            EffectPlaySettings playSettings =
                EffectPlaySettings.GetPlayAtObjectSettings(target, Vector3.zero, Quaternion.identity);
            return effectPlayer.PlayEffect(playSettings);
        }
        
        /// <summary>
        /// Plays effect attached to actor slot
        /// </summary>
        /// <param name="actorSlot"></param>
        /// <param name="effectPlayer"></param>
        /// <returns></returns>
        public Effect PlayEffectAtActorSlot(Transform actorSlot, EffectPlayer effectPlayer)
        {
            if (effectPlayer.IsNull()) return null;
            Vector3 effectDir = CurrentSkillCastData.Direction;
            Quaternion playRot = Quaternion.identity;
            if (effectDir.sqrMagnitude >= 0.001f)
            {
                playRot = Quaternion.LookRotation(effectDir);
            }
            
            //Local rot compared to actorSLot
            Quaternion localRot = Quaternion.Inverse(actorSlot.rotation) * playRot;
            EffectPlaySettings playSettings = EffectPlaySettings.GetPlayAtObjectSettings(actorSlot, Vector3.zero, localRot);

            playSettings.Caster = ParentSkill.ParentSpellBook.Actor;
            return effectPlayer.PlayEffect(playSettings);
        }
        
        /// <summary>
        /// Plays effect at actor slot location (world position)
        /// </summary>
        /// <param name="actorSlot"></param>
        /// <param name="effectPlayer"></param>
        /// <returns></returns>
        public Effect PlayEffectAtActorSlotLocation(Transform actorSlot, EffectPlayer effectPlayer)
        {
            if (effectPlayer.IsNull()) return null;
            Vector3 effectDir = CurrentSkillCastData.Direction;
            Quaternion playRot = Quaternion.identity;
            if (effectDir.sqrMagnitude >= 0.001f)
            {
                playRot = Quaternion.LookRotation(effectDir);
            }
           
            EffectPlaySettings playSettings = EffectPlaySettings.GetPlayAtPositionSettings(actorSlot.position, playRot);

            playSettings.Caster = ParentSkill.ParentSpellBook.Actor;
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
            EffectPlaySettings playSettings = GetEffectPlaySettings(FxPlayData.SkillBehaviourFxPlayType.AtCaster);
            return effectPlayer.PlayEffect(playSettings);
        }

        public Effect PlayEffectOnCaster(EffectPlayer effectPlayer)
        {
            if (effectPlayer.IsNull()) return null;

            EffectPlaySettings playSettings = GetEffectPlaySettings(FxPlayData.SkillBehaviourFxPlayType.OnCaster);
            playSettings.Caster = ParentSkill.ParentSpellBook.Actor;
            //Try to play the effect on actor effect module if possible
            EffectsModule effectModule = ParentSkill.ParentSpellBook.Actor.GetModule<EffectsModule>();
            Effect effect;
            if (effectModule != null)
            {
                effect = effectModule.PlayEffectOnActor(effectPlayer, playSettings.LocalPlayPosition, playSettings.LocalPlayRotation);
                if (effect == null)
                {
                    return null;
                }

                return effect;
            }
            effect = effectPlayer.PlayEffect(playSettings);
            return effect;
        }
        
        public void StopSkillEffects()
        {
            foreach (var effect in PlayedEffects)
            {
                if (effect.OwnerEffectModule != null)
                {
                    effect.OwnerEffectModule.RemoveActiveEffect(effect);
                }
                else
                {
                    effect.Stop(); 
                }
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