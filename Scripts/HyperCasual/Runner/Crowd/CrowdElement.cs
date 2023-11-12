using System;
using UnityEngine;

namespace Kuantech.Core.HyperCasual.Runner
{
    public class CrowdElement : MonoBehaviour
    {
        [Range(0, 1)] public float NormalizedSpeed;
        public bool RequireFormationUpdateOnDespawn;

        [Header("Components")]
        public Animator Animator;

        [NonSerialized] public Crowd ParentCrowd;
        [NonSerialized] public int CrowdIndex;

        #region Lifecycle
        public virtual void Spawn(Crowd parentCrowd)
        {
            ParentCrowd = parentCrowd;
            _currentNormalizeSpeed = 0f;
            NormalizedSpeed = 0f;
        }

        /// <summary>
        /// This removes the crowd element from the crowd
        /// </summary>
        public void Despawn()
        {
            Cleanup();
            if(ParentCrowd != null)
            {
                ParentCrowd.SetCrowdNeedsUpdate(RequireFormationUpdateOnDespawn);
            }
            GameManager.Instance.Pool.PoolObject(gameObject);
        }

        public void Cleanup()
        {

        }
        #endregion

        protected virtual void Update()
        {
            if(ParentCrowd == null) {
                NormalizedSpeed = 0f;
                _currentNormalizeSpeed = 0f;
                return;
            }
            float forward = ParentCrowd.GetMovemenetVector().y;
            NormalizedSpeed = forward;
            SetWalkingAnimation();
        }
        private float _currentNormalizeSpeed;
        [SerializeField] private float AnimationLerpSpeed = 10;
        private void SetWalkingAnimation()
        {
            if(Animator == null) return;
            _currentNormalizeSpeed = Mathf.Lerp(_currentNormalizeSpeed, NormalizedSpeed, Time.deltaTime * AnimationLerpSpeed);
            Animator.SetFloat("MovementSpeed", _currentNormalizeSpeed);
        }
    }
}
