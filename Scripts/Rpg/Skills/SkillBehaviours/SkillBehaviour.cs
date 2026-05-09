using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.FX;
using Kuantech.Networking;
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
            OnCaster,    //Attached to caster
            AtCaster,    //At casters position, without attaching to caster
            AtCastPoint, //At point of cast
            OnTarget,    //On top of the target
        }

        public enum DirectionMode
        {
            None,             // Don't override rotation
            CastDirection,    // Frozen direction from skill cast data
            ActorForward,     // Actor's transform.forward at effect play time
            LiveTarget,       // Recalculated toward live target position
        }

        public SkillBehaviourFxPlayType PlayType;
        public EffectPlayer EffectPlayer;
        public bool StopOnBehaviourEnd;
        [Tooltip("Useful for OnCaster or AtCaster. If play type is OnTarget, effect will be played attached to given slot")]
        public string ActorSlotName;
        [Tooltip("Which direction source to use when rotating the effect")]
        public DirectionMode FxDirectionMode;
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
        public bool WaitForRotationAlign;

        [Header("Locks")]
        public bool LockMovement;
        public float MovementLockDelay;
        public bool LockRotation;
        public float RotationLockDelay;

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
        private bool _implemented;
        private bool _movementLocked;
        private bool _rotationLocked;
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
            _implemented = false;
            _movementLocked = false;
            _rotationLocked = false;

            //Play animation
            PlayBehaviourAnimation();

            OnBehaviourStarted();

            // Apply locks with zero delay immediately (avoids one-frame gap)
            TryApplyLocks(0f);
        }

        protected virtual void PlayBehaviourAnimation()
        {
            AnimationModule am = ParentSkill.ParentSpellBook.Actor.GetModule<AnimationModule>();
            if (am != null)
            {
                am.PlayAnimationData(BehaviourData.BehaviourStartAnimationData, BehaviourData.CastAnimationDuration);
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

                if (effect != null && fx.FxDirectionMode != FxPlayData.DirectionMode.None)
                {
                    effect.transform.forward = fx.FxDirectionMode switch
                    {
                        FxPlayData.DirectionMode.CastDirection => CurrentSkillCastData.Direction,
                        FxPlayData.DirectionMode.ActorForward  => GetParentActor().transform.forward,
                        FxPlayData.DirectionMode.LiveTarget    => GetLiveDirection(),
                        _                                        => GetLiveDirection(),
                    };
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
        
        private void TryApplyLocks(float elapsedTime)
        {
            if (BehaviourData.LockMovement && !_movementLocked && elapsedTime >= BehaviourData.MovementLockDelay)
            {
                Actor actor = GetParentActor();
                if (actor != null)
                {
                    MovementModule mm = actor.GetModule<MovementModule>();
                    if (mm != null) { mm.Lock(this); _movementLocked = true; }
                }
            }
            if (BehaviourData.LockRotation && !_rotationLocked && elapsedTime >= BehaviourData.RotationLockDelay)
            {
                Actor actor = GetParentActor();
                if (actor != null)
                {
                    AimHandler ah = actor.GetModule<AimHandler>();
                    if (ah != null) { ah.LockRotation(this); _rotationLocked = true; }
                }
            }
        }

        private void ReleaseLocks()
        {
            Actor actor = GetParentActor();
            if (actor == null) return;
            if (_movementLocked)
            {
                MovementModule mm = actor.GetModule<MovementModule>();
                if (mm != null) mm.Unlock(this);
                _movementLocked = false;
            }
            if (_rotationLocked)
            {
                AimHandler ah = actor.GetModule<AimHandler>();
                if (ah != null) ah.UnlockRotation(this);
                _rotationLocked = false;
            }
        }

        public void UpdateBehaviour()
        {
            if (_isCompleted) return;
            float duration    = GetDuration();
            float elapsedTime = GetElapsedTime();
            bool  isNetworked = KtNetworkManager.IsNetworked();
            bool  isServer    = ParentSkill.ParentSpellBook.IsServerInitialized;
            bool  isClient    = ParentSkill.ParentSpellBook.IsClientInitialized;

            TryApplyLocks(elapsedTime);

            if (elapsedTime >= BehaviourData.CastTime && !_implemented)
            {
                //Common
                BehaviourImplementation();
                
                if(isServer || !isNetworked)
                {
                    BehaviourServerImplementation();
                }
                if(isClient  || !isNetworked)
                {
                    BehaviourClientImplementation();
                }
                _implemented = true;
            }

            if (duration >= 0 && elapsedTime >= duration)
            {
                CompleteBehaviour();
                return;
            }

            // FX + client prediction — client only. In single-player always runs.
            if (isClient || !isNetworked)
            {
                if (!_playedEffect && elapsedTime >= BehaviourData.EffectPlayTime)
                {
                    PlayBehaviourEffects();
                    _playedEffect = true;
                }
            }
        }

     
        protected virtual void BehaviourImplementation()
        {
        }

        protected virtual void BehaviourServerImplementation()
        {
            
        }
        protected virtual void BehaviourClientImplementation()
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
                case FxPlayData.SkillBehaviourFxPlayType.OnTarget:
                    if (CurrentSkillCastData.Target != null)
                    {
                        playSettings.EffectParent = CurrentSkillCastData.Target.GetActorAnchor();
                        playSettings.LocalPlayPosition = Vector3.zero;
                        playSettings.LocalPlayRotation = localRot;
                    }
                    else
                    {
                        playSettings.SetPosition = true;
                        playSettings.PlayStartPosition = effectStarPosition;
                        playSettings.PlayStartRotation = playRot;
                    }
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
            ReleaseLocks();
            StopSkillEffects();
        }
        
        #region Cast Position Helpers

        /// <summary>
        /// Returns the caster's CURRENT cast slot position at the moment of execution.
        /// Use this instead of CurrentSkillCastData.StartPosition in BehaviourImplementation
        /// to avoid stale positions when there is a CastTime delay (actor may have moved/rotated).
        /// Direction and TargetPosition remain frozen (aim is locked at input time).
        /// </summary>
        protected Vector3 GetLiveStartPosition()
        {
            return ParentSkill.ParentSpellBook.GetDefaultCastPosition();
        }

        /// <summary>
        /// Recalculates direction from live start position toward the frozen target/aim point.
        /// Falls back to the frozen direction if no target or target position is set.
        /// </summary>
        protected Vector3 GetLiveDirection()
        {
            Vector3 liveStart = GetLiveStartPosition();
            if (CurrentSkillCastData.Target != null)
            {
                Vector3 toTarget = CurrentSkillCastData.Target.transform.position - liveStart;
                if (toTarget.sqrMagnitude > 0.001f) return toTarget.normalized;
            }
            Vector3 toPoint = CurrentSkillCastData.TargetPosition - liveStart;
            if (toPoint.sqrMagnitude > 0.001f) return toPoint.normalized;
            return CurrentSkillCastData.Direction; // frozen fallback
        }

        #endregion

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