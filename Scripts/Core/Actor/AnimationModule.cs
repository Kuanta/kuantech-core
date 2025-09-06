using System;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Core
{
    /// <summary>
    /// A common animation module for common actor events
    /// </summary>
    public class AnimationModule : ActorModule
    {
        public AnimatorOverrideController DefaultAnimationSet;
        public Animator Animator;

        [Header("Settings")]
        [Tooltip("If set to true, movement will be sent to animator as a single float")]
        public bool UseOneDimensionalMovement;
        
        private Vector2 _targetMovementParameters = Vector2.zero;
        private Vector2 _movementParameters = Vector2.zero;
        private Vector2 _movementParametersScale = Vector2.one;

        public float LerpFactor = 10f;
    
        //Events
        public UnityEvent OnDamageFrameEvent;
        
        //Animation Hashes
        private static readonly int X = Animator.StringToHash("Forward");
        private static readonly int Y = Animator.StringToHash("Right");
        private static readonly int Movement = Animator.StringToHash("Movement");
        private static readonly int Attack = Animator.StringToHash("Attack");
        private static readonly int Hold = Animator.StringToHash("Hold");
        private static readonly int AttackIndex = Animator.StringToHash("AttackIndex");
        private static readonly int HandIndex = Animator.StringToHash("HandIndex");
        public static readonly int AttackSpeed = Animator.StringToHash("AttackSpeed");
        public static readonly int TargetTime = Animator.StringToHash("TargetTime");
        private static readonly int Death = Animator.StringToHash("Dead");
        private static readonly int DamageReceived = Animator.StringToHash("DamageReceived");
        private static readonly int DamageReceivedIndex = Animator.StringToHash("DamageReceivedIndex");
        private static readonly int Aiming = Animator.StringToHash("Aiming");
        private static readonly int AlternativeAttack = Animator.StringToHash("AlternativeAttack");
        private static readonly int Jump = Animator.StringToHash("Jump");
        private static readonly int Land = Animator.StringToHash("Land");
        private static readonly int Cast = Animator.StringToHash("Cast");
        private static readonly int CastIndex = Animator.StringToHash("CastIndex");
        private static readonly int AttackSpeedMultiplier = Animator.StringToHash("AttackSpeedMultiplier");


        public override void Initialize()
        {
            base.Initialize();
            ApplyDefaultAnimationSet();
            Actor.OnHitEvent += OnDamageReceive;
        }
        
        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            RigidbodyMovementModule mm = Actor.GetModule<RigidbodyMovementModule>();
            if (mm != null)
            {
                mm.OnJumpEvent += OnJump;
                mm.OnJumpLandEvent += OnLand;
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
            
            //Update from Motion Vectors Handler
            if (Actor.MotionVectorsHandler != null)
            {
                UpdateMovementParameters();
            }
            
            _movementParameters =
                Vector2.Lerp(_movementParameters, _targetMovementParameters * _movementParametersScale, Time.deltaTime * LerpFactor);
           
            if (Animator == null) return;
            SetMovementParameters();
        }
        
        private void SetMovementParameters()
        {
            if(UseOneDimensionalMovement)
            {
                Animator.SetFloat(Movement, _movementParameters.magnitude);
            }
            else
            {
                Animator.SetFloat(X, _movementParameters.x);
                Animator.SetFloat(Y, _movementParameters.y);
            }
        }

        public Animator GetAnimator()
        {
            return Animator;
        }

        #region Animation Sets
        public void ApplyDefaultAnimationSet()
        {
            if (DefaultAnimationSet == null || Animator == null) return; 
            Animator.runtimeAnimatorController = DefaultAnimationSet;
        }

        #endregion

        #region Animation Play
        public void SetClip(string clipName, AnimationClip clip)
        {
            Animator animator = GetAnimator();
            if (animator == null) return;
        }
        public void SetTrigger(int hash)
        {
            Animator.SetTrigger(hash);
        }

        public void PlayAnimation(AnimationData animationData, float speedMultiplier = 1.0f)
        {
            Animator animator = GetAnimator();
            if (animator == null) return;
            animationData.SetParameters(animator);
            animator.SetFloat(AttackSpeedMultiplier, speedMultiplier);
        }
        #endregion
  
        public override void Reset()
        {
            base.Reset();
            if (Animator != null)
            {            
                Animator.SetFloat(X, 0);
                Animator.SetFloat(Y, 0);
                Animator.SetBool(Death, false);
                Animator.Rebind();
            }
            _targetMovementParameters = Vector2.zero;
            _movementParametersScale = Vector2.one;
        }
        
        #region Movement

        private void UpdateMovementParameters()
        {
            Vector3 localMovement = Actor.MotionVectorsHandler.GetLocalMovementVector();
            _targetMovementParameters = new Vector2(localMovement.x, localMovement.z);
        }
        
        public void SetMovementParametersFromMovementDirection(Vector3 direction, bool forced = false)
        {
            Vector2 movement = Helpers.GetVector2FromVector3WithUpDirection(direction, Actor.ActorUpVector);
            SetMovementParameters(movement, forced);
        }
        
        public void SetMovementParameters(Vector2 movement, bool forced = false)
        {
            movement.Normalize();
            if (Actor.GetModule<RigidbodyMovementModule>() != null && Actor.GetModule<RigidbodyMovementModule>().IsDodging())
            {
                if (movement.magnitude < 0.01f)
                {
                    movement = Vector2.up;
                }
                movement = movement.normalized * 2;
            }
            _targetMovementParameters = movement;
            if (!forced) return;
            _movementParameters.x = movement.x *  _movementParametersScale.x;
            _movementParameters.y = movement.y * _movementParametersScale.y;

        }
        
        public void SetMovementParameters(float side, float forward,  bool forced = false)
        {
            SetMovementParameters(new Vector2(side, forward), forced);
        }
        
        public void SetMovementParametersScale(Vector2 scale)
        {
            _movementParametersScale = scale;
        }
        public void OnJump(object sender, EventArgs args)
        {
            Animator.SetTrigger(Jump);
        }

        public void OnLand(object sender, EventArgs args)
        {
            Animator.SetTrigger(Land);
        }
        #endregion
        
        #region Combat
        /// <summary>
        /// Starts a light attack given the hand and attack(flow) index
        /// </summary>
        /// <param name="handIndex"></param>
        /// <param name="attackIndex"></param>
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
            Animator.SetBool(Hold, false);
        }
        
        /// <summary>
        /// Can be called from an animation where damage should be applied. Works for client authorotive games but for
        /// server authoration, this should be handled by a timer on the server
        /// </summary>
        public void OnDamageFrame()
        {
            OnDamageFrameEvent?.Invoke();
        }

        public void SetAnimationTime(float animationTime)
        {
            if (Animator == null) return;
            Animator.SetFloat(TargetTime, animationTime);
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
        #endregion

        #region Events

        public override void OnActorStateChanged(ActorState oldState, ActorState newState)
        {
            base.OnActorStateChanged(oldState, newState);
            if (Animator == null) return;
            if (newState == ActorState.Dead)
            {
                Animator.SetBool(Death, true);
            }else if (newState == ActorState.Spawned)
            {
                Animator.SetBool(Death, false);
            }
        }
        public void OnDamageReceive(HitInfo hitInfo)
        {
            if (Animator == null) return;
            Animator.SetInteger(DamageReceivedIndex, 0);
            Animator.SetTrigger(DamageReceived);
        }

        public void OnActorVisualChanged(ActorVisual newVisual)
        {
            if (newVisual == null)
            {
                //Set Animator to null
                Animator = null;
                return;
            }
            Animator = newVisual.Animator;
            if(Animator != null) Animator.logWarnings = false;
        }

        #endregion


    }
}