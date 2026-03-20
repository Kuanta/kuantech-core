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

        [Header("Settings")]
        [Tooltip("Send movement as a single magnitude float instead of Forward/Sideways")]
        public bool UseOneDimensionalMovement;

        [Header("Animation Parameters")]
        [SerializeField] private AnimationData DamageReceivedAnimationData;
        [SerializeField] private AnimationData DodgeAnimationData;

        public float LerpFactor = 10f;

        // Events
        public UnityEvent OnDamageFrameEvent;

        // Cached modules
        private MovementModule _movementModule;

        // Movement blend parameters
        private Vector2 _targetMovementParameters = Vector2.zero;
        private Vector2 _movementParameters = Vector2.zero;
        private Vector2 _movementParametersScale = Vector2.one;

        [NonSerialized] public bool IsGroundedFlag;
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
            }

            ActorVisualHandler visualHandler = Actor.GetModule<ActorVisualHandler>();
            if (visualHandler != null)
            {
                visualHandler.OnActorVisualSet += OnActorVisualChanged;
                OnActorVisualChanged(visualHandler.CurrentActorVisual);
            }
        }

        public override void ModuleUpdate()
        {
            if (GameManager.Instance.GameIsPaused || Animator == null || Actor == null) return;

            if (Actor.MotionVectorsHandler != null)
                UpdateMovementParameters();

            _movementParameters = Vector2.Lerp(
                _movementParameters,
                _targetMovementParameters * _movementParametersScale,
                Time.deltaTime * LerpFactor);

            SetMovementParameters();
        }

        public override void ResetModule()
        {
            base.ResetModule();
            if (Animator != null)
            {
                Animator.SetFloat(Forward, 0);
                Animator.SetFloat(Sideways, 0);
                Animator.SetBool(Death, false);
                Animator.SetFloat(IsGrounded, 1f);
                Animator.SetFloat(AirTimeHash, 0f);
                Animator.Rebind();
            }
            _targetMovementParameters = Vector2.zero;
            _movementParametersScale = Vector2.one;
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
        }

        private void SetMovementParameters()
        {
            if (UseOneDimensionalMovement)
                Animator.SetFloat(Movement, _movementParameters.magnitude);
            else
            {
                Animator.SetFloat(Sideways, _movementParameters.x);
                Animator.SetFloat(Forward, _movementParameters.y);
            }
            Animator.SetFloat(IsGrounded, IsGroundedFlag ? 1f : 0f);
            Animator.SetFloat(AirTimeHash, AirTime);
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
            if (Animator == null) return;
            Animator.SetBool(Crouching, toggle);
        }

        // Movement event callbacks — subscribed in OnModulesInitialized
        public void OnJump(object sender, EventArgs args)
        {
            if (Animator == null) return;
            Animator.SetTrigger(Jump);
        }

        public void OnLand(object sender, EventArgs args)
        {
            if (Animator == null) return;
            Animator.SetTrigger(Land);
        }

        private void OnDash(object sender, Vector3 direction)
        {
            if (Animator == null) return;
            Animator.SetTrigger(Dash);
        }

        private void OnDodge(object sender, EventArgs args)
        {
            if (Animator == null || DodgeAnimationData == null) return;
            float duration = args is DodgeEventArgs dodge ? dodge.Duration : -1;
            PlayAnimationData(DodgeAnimationData, duration);
        }

        // ─── Combat ───────────────────────────────────────────────────────────────

        public void LightAttackTrigger(int handIndex = 0, int attackIndex = 0)
        {
            if (Animator == null) return;
            Animator.SetTrigger(Attack);
            Animator.SetBool(Hold, false);
            Animator.SetInteger(HandIndex, handIndex);
            Animator.SetInteger(AttackIndex, attackIndex);
        }

        public void AlternativeAttackTrigger(int handIndex = 0, int attackIndex = 0)
        {
            if (Animator == null) return;
            Animator.SetTrigger(AlternativeAttack);
            Animator.SetBool(Hold, false);
            Animator.SetInteger(HandIndex, handIndex);
            Animator.SetInteger(AttackIndex, attackIndex);
        }

        public void SetRelease()
        {
            if (Animator == null) return;
            Animator.SetBool(Hold, false);
        }

        public void ToggleAiming(bool toggle)
        {
            if (Animator == null) return;
            Animator.SetBool(Aiming, toggle);
        }

        public void SkillCast(int castIndex = 0)
        {
            if (Animator == null) return;
            Animator.SetInteger(CastIndex, castIndex);
            Animator.SetTrigger(Cast);
        }

        public void SetAnimationTime(float animationTime)
        {
            if (Animator == null) return;
            Animator.SetFloat(TargetTime, animationTime);
        }

        public void SetTrigger(int hash)
        {
            if (Animator == null) return;
            Animator.SetTrigger(hash);
        }

        // ─── Animation data / montage ─────────────────────────────────────────────

        public void PlayAnimationData(AnimationData animationData, float animationDuration = -1)
        {
            if (Animator == null) return;
            animationData.SetParameters(Animator);
            if (animationDuration > 0)
                Animator.SetFloat(TargetTime, animationDuration);
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
            if (Animator == null) return;
            if (newState == ActorState.Dead)
                Animator.SetBool(Death, true);
            else if (newState == ActorState.Spawned)
                Animator.SetBool(Death, false);
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
