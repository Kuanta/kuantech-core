using System;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Core
{
    /// <summary>
    /// Drives an Animator from actor events and MotionVectorsHandler data.
    /// Animators must follow the parameter naming convention defined by the hash constants below.
    /// </summary>
    public class AnimationModule : ActorModule
    {
        public RuntimeAnimatorController DefaultAnimationSet;
        public Animator Animator;
        public AnimationMontagePlayer MontagePlayer;

        /// <summary>
        /// Where parameter writes go when the actor has no Animator — a GPU-baked crowd agent, for example.
        /// Set by whatever provides the alternative animation system; null for ordinary actors, in which case
        /// this module behaves exactly as it always has. Callers never need to know which one is in use.
        /// </summary>
        [NonSerialized] public IAnimationDriver Driver;

        /// <summary>True when there is anything to animate at all, by either route.</summary>
        public bool HasAnimationTarget => Animator != null || Driver != null;

        [Header("Settings")]
        [Tooltip("Send movement as a single magnitude float instead of Forward/Sideways")]
        public bool UseOneDimensionalMovement;

        [Header("Animation Parameters")]
        [SerializeField] private AnimationData DamageReceivedAnimationData;

        public float LerpFactor = 10f;

        // Events
        public UnityEvent OnDamageFrameEvent;

        // Cached modules
        private MovementModule _movementModule;

        // Movement blend parameters
        private Vector2 _targetMovementParameters = Vector2.zero;
        private Vector2 _movementParameters = Vector2.zero;
        private Vector2 _movementParametersScale = Vector2.one;

        [NonSerialized] public bool IsGroundedFlag = true;
        [NonSerialized] public float AirTime;

        // Animation parameter hashes — animator must use these exact parameter names
        private static readonly int Forward          = Animator.StringToHash("Forward");
        private static readonly int Sideways         = Animator.StringToHash("Right");
        private static readonly int Movement         = Animator.StringToHash("Movement");
        private static readonly int Death            = Animator.StringToHash("Dead");
        private static readonly int Aiming          = Animator.StringToHash("Aiming");
        private static readonly int Jump             = Animator.StringToHash("Jump");
        private static readonly int Land             = Animator.StringToHash("Land");
        private static readonly int Dash             = Animator.StringToHash("Dash");
        private static readonly int Crouching        = Animator.StringToHash("Crouching");
        private static readonly int IsGrounded       = Animator.StringToHash("IsGrounded");
        private static readonly int AirTimeHash      = Animator.StringToHash("AirTime");
        private static readonly int Attack           = Animator.StringToHash("Attack");
        private static readonly int AlternativeAttack= Animator.StringToHash("AlternativeAttack");
        private static readonly int Hold             = Animator.StringToHash("Hold");
        private static readonly int AttackIndex      = Animator.StringToHash("AttackIndex");
        private static readonly int HandIndex        = Animator.StringToHash("HandIndex");
        private static readonly int Cast             = Animator.StringToHash("Cast");
        private static readonly int CastIndex        = Animator.StringToHash("CastIndex");
        public static readonly int AttackSpeed       = Animator.StringToHash("AttackSpeed");
        public static readonly int TargetTime        = Animator.StringToHash("TargetTime");

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public override void Initialize()
        {
            base.Initialize();
            ApplyDefaultAnimationSet();
        }

        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();

            _movementModule = Actor.GetModule<MovementModule>();
            if (_movementModule != null)
            {
                _movementModule.OnJumpEvent     += OnJump;
                _movementModule.OnJumpLandEvent += OnLand;
                _movementModule.DashStartEvent  += OnDash;
                _movementModule.DashEndEvent    += OnDashEnd;
                _movementModule.CrouchStarted += OnCrouchStarted;
                _movementModule.CrouchEnded     += OnCrouchEnded;
            }

            ActorVisualHandler visualHandler = Actor.GetModule<ActorVisualHandler>();
            if (visualHandler != null)
            {
                visualHandler.OnActorVisualSet += OnActorVisualChanged;
                OnActorVisualChanged(visualHandler.CurrentActorVisual);
            }
        }

        public override void ModuleUpdate(float deltaTime)
        {
            if (GameManager.Instance.GameIsPaused || !HasAnimationTarget || Actor == null) return;

            if(_movementModule != null)
            {
                IsGroundedFlag = _movementModule.IsGrounded();
                AirTime = _movementModule.GetAirTime();
                //Animator.SetBool(Crouching, _movementModule.Crouching); //todo: temp fix. Implement transitions
            }
            if (Actor.MotionVectorsHandler != null)
            {
                UpdateMovementParameters();
            }

            _movementParameters = Vector2.Lerp(
                _movementParameters,
                _targetMovementParameters * _movementParametersScale,
                deltaTime * LerpFactor);

            SetMovementParameters();
        }

        public override void ResetModule()
        {
            base.ResetModule();
            if (Animator != null)
            {
                // Animator.SetFloat(Forward, 0);
                // Animator.SetFloat(Sideways, 0);
                // Animator.SetBool(Death, false);
                // Animator.SetFloat(IsGrounded, 1f);
                // Animator.SetFloat(AirTimeHash, 0f);
                Animator.Rebind();
            }
            _targetMovementParameters = Vector2.zero;
            _movementParametersScale = Vector2.one;
        }

        // ─── Parameter writes ─────────────────────────────────────────────────────
        // Every parameter write goes through these four so the Animator and the driver stay interchangeable.
        // The Animator wins when both exist; a driver is a substitute, not an addition.

        private void WriteFloat(int hash, float value)
        {
            if (Animator != null) Animator.SetFloat(hash, value);
            else Driver?.SetFloat(hash, value);
        }

        private void WriteBool(int hash, bool value)
        {
            if (Animator != null) Animator.SetBool(hash, value);
            else Driver?.SetBool(hash, value);
        }

        private void WriteTrigger(int hash)
        {
            if (Animator != null) Animator.SetTrigger(hash);
            else Driver?.SetTrigger(hash);
        }

        private void WriteInteger(int hash, int value)
        {
            if (Animator != null) Animator.SetInteger(hash, value);
            else Driver?.SetInteger(hash, value);
        }

        // ─── Animator reference ───────────────────────────────────────────────────

        public Animator GetAnimator() => Animator;

        public void OnActorVisualChanged(ActorVisual newVisual)
        {
            if (newVisual == null)
            {
                Animator = null;
                return;
            }
            Animator = newVisual.Animator;
            if (Animator != null) Animator.logWarnings = false;
            if (MontagePlayer != null) MontagePlayer.Animator = Animator;
        }

        // ─── Animation sets ───────────────────────────────────────────────────────

        public void ApplyDefaultAnimationSet()
        {
            if (DefaultAnimationSet == null || Animator == null) return;
            Animator.runtimeAnimatorController = DefaultAnimationSet;
        }

        public void ApplyAnimationSet(RuntimeAnimatorController animationSet)
        {
            if (animationSet == null || Animator == null) return;
            Animator.runtimeAnimatorController = animationSet;
        }

        // ─── Movement ─────────────────────────────────────────────────────────────

        private void UpdateMovementParameters()
        {
            // GetLocalMovementVector() is already normalized (~magnitude 1).
            // GetNormalizedSpeed() returns 0..1 at base speed, up to SprintMultiplier when sprinting.
            // Do NOT also multiply by GetMovementMultiplier() — it's already inside GetNormalizedSpeed().
            Vector3 localMovement = Actor.MotionVectorsHandler.GetLocalMovementVector();

            if (_movementModule != null)
                localMovement *= _movementModule.GetNormalizedSpeed();

            _targetMovementParameters = new Vector2(localMovement.x, localMovement.z);

            Debug.Log($"[AnimationModule] {Actor.name} IsOwner={IsOwner} IsServer={IsServer} " +
                      $"rawMovement={Actor.MotionVectorsHandler.GetMovementVector()} " +
                      $"rawTarget={Actor.MotionVectorsHandler.GetTargetVector()} " +
                      $"speed={(_movementModule != null ? _movementModule.GetNormalizedSpeed() : -1f)} " +
                      $"localMovement={localMovement}");
        }

        private void SetMovementParameters()
        {
            if (UseOneDimensionalMovement)
                WriteFloat(Movement, _movementParameters.magnitude);
            else
            {
                WriteFloat(Sideways, _movementParameters.x);
                WriteFloat(Forward, _movementParameters.y);
            }
            // Animator.SetFloat(IsGrounded, IsGroundedFlag ? 1f : 0f);
            // Animator.SetFloat(AirTimeHash, AirTime);
        }

        public void SetMovementParametersFromMovementDirection(Vector3 direction, bool forced = false)
        {
            Vector2 movement = Helpers.GetVector2FromVector3WithUpDirection(direction, Actor.ActorUpVector);
            SetMovementParameters(movement, forced);
        }

        public void SetMovementParameters(Vector2 movement, bool forced = false)
        {
            movement.Normalize();
            _targetMovementParameters = movement * Actor.MotionVectorsHandler.GetMovementMultiplier();
            if (!forced) return;
            _movementParameters.x = movement.x * _movementParametersScale.x;
            _movementParameters.y = movement.y * _movementParametersScale.y;
        }

        public void SetMovementParameters(float side, float forward, bool forced = false)
            => SetMovementParameters(new Vector2(side, forward), forced);

        public void SetMovementParametersScale(Vector2 scale) => _movementParametersScale = scale;

        public void ToggleCrouching(bool toggle)
        {
            if (!HasAnimationTarget) return;
            WriteBool(Crouching, toggle);
        }

        // Movement event callbacks — subscribed in OnModulesInitialized
        public void OnJump(object sender, EventArgs args)
        {
            if (!HasAnimationTarget) return;
            WriteTrigger(Jump);
        }

        public void OnLand(object sender, EventArgs args)
        {
            if (!HasAnimationTarget) return;
            WriteTrigger(Land);
        }

        private void OnDash(object sender, Vector3 direction)
        {
            if (!HasAnimationTarget) return;
            WriteTrigger(Dash);
        }

        private void OnDashEnd(object sender, EventArgs args)
        {
            if (!HasAnimationTarget) return;
        }

        private void OnCrouchStarted(object sender, EventArgs args)
        {
            if (!HasAnimationTarget) return;
            WriteBool(Crouching, true);
        }

        private void OnCrouchEnded(object sender, EventArgs args)
        {
            if (!HasAnimationTarget) return;
            WriteBool(Crouching, false);
        }
        // ─── Combat ───────────────────────────────────────────────────────────────

        public void LightAttackTrigger(int handIndex = 0, int attackIndex = 0)
        {
            if (!HasAnimationTarget) return;
            WriteTrigger(Attack);
            WriteBool(Hold, false);
            WriteInteger(HandIndex, handIndex);
            WriteInteger(AttackIndex, attackIndex);
        }

        public void AlternativeAttackTrigger(int handIndex = 0, int attackIndex = 0)
        {
            if (!HasAnimationTarget) return;
            WriteTrigger(AlternativeAttack);
            WriteBool(Hold, false);
            WriteInteger(HandIndex, handIndex);
            WriteInteger(AttackIndex, attackIndex);
        }

        public void SetRelease()
        {
            if (!HasAnimationTarget) return;
            WriteBool(Hold, false);
        }

        public void ToggleAiming(bool toggle)
        {
            if (!HasAnimationTarget) return;
            WriteBool(Aiming, toggle);
        }

        public void SkillCast(int castIndex = 0)
        {
            if (!HasAnimationTarget) return;
            WriteInteger(CastIndex, castIndex);
            WriteTrigger(Cast);
        }

        public void SetAnimationTime(float animationTime)
        {
            if (!HasAnimationTarget) return;
            WriteFloat(TargetTime, animationTime);
        }

        public void SetTrigger(int hash)
        {
            if (!HasAnimationTarget) return;
            WriteTrigger(hash);
        }

        public void SetBoolean(int hash, bool value)
        {
            if (!HasAnimationTarget) return;
            WriteBool(hash, value);
        }

        // ─── Animation data / montage ─────────────────────────────────────────────

        public void PlayAnimationData(AnimationData animationData, float animationDuration = -1)
        {
            if (!HasAnimationTarget) return;

            if (Animator != null) animationData.SetParameters(Animator);
            else animationData.SetParameters(Driver);

            if (animationDuration > 0)
                WriteFloat(TargetTime, animationDuration);
            if (animationData.AttackMontage != null)
                PlayAnimationMontage(animationData.AttackMontage, animationDuration);
        }

        private void PlayAnimationMontage(AnimationMontage animationMontage, float targetTime = 1.0f)
        {
            if (Animator == null || MontagePlayer == null) return;
            Animator.SetFloat(TargetTime, targetTime);
            MontagePlayer.PlayMontage(animationMontage);
        }

        // ─── Actor state / damage ─────────────────────────────────────────────────

        public override void OnActorStateChanged(ActorState oldState, ActorState newState)
        {
            base.OnActorStateChanged(oldState, newState);
            if (!HasAnimationTarget) return;
            if (newState == ActorState.Dead)
                WriteBool(Death, true);
            else if (newState == ActorState.Spawned)
                WriteBool(Death, false);
        }

        public void OnDamageReceive(HitInfo hitInfo)
        {
            PlayAnimationData(DamageReceivedAnimationData);
        }

        /// <summary>
        /// Called from an animation event when the damage frame is reached.
        /// For server-authoritative games use a server-side timer instead.
        /// </summary>
        public void OnDamageFrame()
        {
            OnDamageFrameEvent?.Invoke();
        }
    }
}
