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

        // Effects played with FxDirectionMode.LiveTarget — re-oriented every tick (see
        // UpdateLiveDirectionEffects) so they keep facing wherever GetLiveDirection() currently points,
        // instead of freezing at whatever it was the instant the effect spawned.
        private readonly List<Effect> _liveDirectionEffects = new List<Effect>();
        
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

        public SkillBehaviourConfigData GetConfigData()
        {
            return BehaviourData.ConfigData;
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
            _liveDirectionEffects.Clear();
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

                    // LiveTarget isn't a one-time aim — keep it tracking every tick for as long as the
                    // effect is alive, same as the damage/hit logic already does.
                    if (fx.FxDirectionMode == FxPlayData.DirectionMode.LiveTarget)
                    {
                        _liveDirectionEffects.Add(effect);
                    }
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

            // Runs every frame once the cast has resolved, for as long as the behaviour stays active — for
            // a channeled effect (a flamethrower cone, a beam) that needs to act repeatedly across Duration
            // rather than once at CastTime like BehaviourImplementation. Empty by default: every existing
            // one-shot behaviour is unaffected.
            if (_implemented)
            {
                OnBehaviourUpdate(elapsedTime);
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
                UpdateLiveDirectionEffects();
            }
        }

        /// <summary>
        /// Re-aims every effect played with FxDirectionMode.LiveTarget at the current GetLiveDirection(),
        /// every frame. A no-op until PlayBehaviourEffects has actually spawned one.
        /// </summary>
        private void UpdateLiveDirectionEffects()
        {
            if (_liveDirectionEffects.Count == 0) return;

            Vector3 liveDirection = GetLiveDirection();
            for (int i = _liveDirectionEffects.Count - 1; i >= 0; i--)
            {
                Effect effect = _liveDirectionEffects[i];
                if (effect == null) { _liveDirectionEffects.RemoveAt(i); continue; }
                effect.transform.forward = liveDirection;
            }
        }

     
        protected virtual void BehaviourImplementation()
        {
        }

        /// <summary>
        /// Called every frame from CastTime until the behaviour completes — the hook for a channeled effect
        /// that needs to do something repeatedly (e.g. tick damage on an interval it tracks itself) rather
        /// than once. <paramref name="elapsedTime"/> is time since StartBehaviour, same clock as GetDuration.
        /// </summary>
        protected virtual void OnBehaviourUpdate(float elapsedTime)
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
        /// Recalculates direction from the live start position toward the current aim point. Prefers
        /// CurrentSkillCastData.LiveAimPointProvider when the caster supplied one (re-evaluated every call,
        /// so it can retarget entirely — not just follow the original target moving); otherwise follows the
        /// frozen Target's live position, then the frozen TargetPosition, then the frozen Direction.
        /// </summary>
        protected Vector3 GetLiveDirection()
        {
            Vector3 liveStart = GetLiveStartPosition();

            if (CurrentSkillCastData.LiveAimPointProvider != null)
            {
                Vector3 dir = FlattenToDirection(CurrentSkillCastData.LiveAimPointProvider() - liveStart);
                if (dir != Vector3.zero) return dir;
            }
            else if (CurrentSkillCastData.Target != null)
            {
                Vector3 dir = FlattenToDirection(CurrentSkillCastData.Target.transform.position - liveStart);
                if (dir != Vector3.zero) return dir;
            }

            Vector3 toPointDir = FlattenToDirection(CurrentSkillCastData.TargetPosition - liveStart);
            if (toPointDir != Vector3.zero) return toPointDir;
            return CurrentSkillCastData.Direction; // frozen fallback
        }

        /// <summary>
        /// Zeroes Y before normalizing — every other direction calc in this project is horizontal-only (the
        /// caster's cast point sits above the ground, so an un-flattened vector to a ground-level target
        /// tilts downward and can push it clean out of an arc/cone check). Returns Vector3.zero if the
        /// flattened vector is too short to have a meaningful direction.
        /// </summary>
        private static Vector3 FlattenToDirection(Vector3 toTarget)
        {
            toTarget.y = 0f;
            return toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : Vector3.zero;
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