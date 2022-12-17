using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Core
{
    public class AnimatorModule : Module
    {
        [SerializeField] private Animator Animator;
        private static readonly int X = Animator.StringToHash("Forward");
        private static readonly int Y = Animator.StringToHash("Right");

        private Vector2 _targetMovementParameters = Vector2.zero;
        private Vector2 _movementParameters = Vector2.zero;

        public float LerpFactor = 10f;
        
        //Events
        public UnityEvent OnDamageFrameEvent;
        private static readonly int Attack = Animator.StringToHash("Attack");
        private static readonly int Hold = Animator.StringToHash("Hold");
        private static readonly int AttackIndex = Animator.StringToHash("AttackIndex");
        private static readonly int HandIndex = Animator.StringToHash("HandIndex");
        public static readonly int AttackSpeed = Animator.StringToHash("AttackSpeed");
        public static readonly int TargetTime = Animator.StringToHash("TargetTime");

        private void Update()
        {
            _movementParameters =
                Vector2.Lerp(_movementParameters, _targetMovementParameters, Time.deltaTime * LerpFactor);
            Animator.SetFloat(X, _movementParameters.x);
            Animator.SetFloat(Y,_movementParameters.y);
        }

        public void SetMovementParameters(Vector2 movement)
        {
            _targetMovementParameters = movement;
        }

       
        public void SetTrigger(int hash)
        {
            Animator.SetTrigger(hash);
        }

        public override void Reset()
        {
            base.Reset();
            Animator.Rebind();
            Animator.SetFloat(X, 0);
            Animator.SetFloat(Y, 0);
            _targetMovementParameters = Vector2.zero;
        }
        
        #region Combat
        /// <summary>
        /// Starts a light attack given the hand and attack(flow) index
        /// </summary>
        /// <param name="handIndex"></param>
        /// <param name="attackIndex"></param>
        public void LightAttackTrigger(int handIndex = 0, int attackIndex = 0)
        {
            Animator.SetTrigger(Attack);
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
            Animator.SetFloat(TargetTime, animationTime);
        }
        #endregion
    }
}