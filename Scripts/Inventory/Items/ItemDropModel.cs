using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Inventory.Items
{   
    /// <summary>
    /// Drop model for an item. 
    /// </summary>
    public class ItemDropModel : MonoBehaviour
    {
        public Kuantech.Physics.Rigidbody Rigidbody;
        
        //Target follow
        private bool _followTarget;
        private Transform _target;
        private Vector3 _targetOffset;
        private float _followSpeed = 20f;
        private float _followThreshold = 0.1f;
        private UnityAction<ItemDropModel> _followCompleteHandler;
        
        private void Update()
        {
            if (_followTarget && _target != null)
            {
                Vector3 dist = (_target.position + _targetOffset) - transform.position;
                if (dist.sqrMagnitude <= _followThreshold * _followThreshold)
                {
                    _followCompleteHandler?.Invoke(this);
                    _followTarget = false;
                    return;
                }
                dist.Normalize();
                transform.position += dist * _followSpeed * Time.deltaTime;
            }
        }
        
        public float SetTrajectory(float verticalDistance, float horizontalDistance, Vector2 direction,
            float acceleration)
        {
            return Rigidbody.SetTrajectory(verticalDistance, horizontalDistance, direction, acceleration);
        }
        
        /// <summary>
        /// Sets the drop to go towards a target
        /// </summary>
        /// <param name="transform">Target to follow</param>
        /// <param name="targetOffset"></param>
        /// <param name="delay"></param>
        public void GoToTarget(Transform transform, Vector3 targetOffset, float delay, UnityAction<ItemDropModel> reachAction)
        {
            _target = transform;
            _targetOffset = targetOffset;
            _followCompleteHandler = reachAction;
            StartCoroutine(GoToTargetRoutine(delay));
        }

        private IEnumerator GoToTargetRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            _followTarget = true;
        }
    }
}