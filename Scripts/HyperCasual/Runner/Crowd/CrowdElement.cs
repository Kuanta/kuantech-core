using System;
using UnityEngine;

namespace Kuantech.Core.HyperCasual.Runner
{
    public class CrowdElement : MonoBehaviour
    {
        [Range(0, 1)] public float NormalizedSpeed;

        [Header("Components")]
        public Animator Animator;

        [NonSerialized] public Crowd ParentCrowd;
        [NonSerialized] public int CrowdIndex;

        #region Lifecycle
        public virtual void Spawn(Crowd parentCrowd)
        {
            ParentCrowd = parentCrowd;
        }

        /// <summary>
        /// This removes the crowd element from the crowd
        /// </summary>
        public void Despawn()
        {
            Cleanup();
            if(ParentCrowd != null)
            {
                ParentCrowd.SetCrowdNeedsUpdate();
            }
            GameManager.Instance.Pool.PoolObject(gameObject);
        }

        public void Cleanup()
        {

        }
        #endregion

        protected virtual void Update()
        {
            if(ParentCrowd == null) return;
            float forward = ParentCrowd.GetMovemenetVector().y;
            NormalizedSpeed = forward;
            SetWalkingAnimation();
        }

        private void SetWalkingAnimation()
        {
            Animator.SetFloat("MovementSpeed", NormalizedSpeed);
        }
    }
}
