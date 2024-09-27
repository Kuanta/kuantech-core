using System;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Utils
{
    /// <summary>
    /// A component that can follow a throwable trajectory
    /// </summary>
    public class ThrowableTrajectoryFollower : MonoBehaviour
    {
        public AnimationCurve SpeedCurve;
        public float Speed = 5f;  // Speed at which the resource should fly
        public float InitialRiseSpeed = 25.0f;
        public float MaxRiseHeight = 3.0f;
        public float ReachThresh = 0.1f;

        [NonSerialized] public bool IsMoving = false;
        [NonSerialized] public WorldPoint TargetPoint;
        [NonSerialized] public UnityAction ReachedTargetHandler;
        
        private float _currentRiseSpeed;
        private float _currentRiseHeight;
        private int _currentRiseDir;
       
        public void FlyToTarget(WorldPoint targetPoint, UnityAction reachedTargetHandler = null)
        {
            ReachedTargetHandler = reachedTargetHandler;
            TargetPoint = targetPoint;
            IsMoving = true;
            transform.SetParent(null, true);
            Vector3 targetPosition = TargetPoint.GetTargetPosition();
            _currentRiseSpeed = InitialRiseSpeed;
            _currentRiseHeight = 0;
            _currentRiseDir = 1;
        }
        
        private void Update()
        {
            if (!IsMoving || TargetPoint == null) return;

            // Calculate the desired position with offset
            Vector3 targetPosition = TargetPoint.GetTargetPosition();
            float distance = Vector3.Distance(transform.position, targetPosition);
            float normalizedSpeed = SpeedCurve.Evaluate(distance);

            // Apply initial rise speed and decay it
            _currentRiseHeight += _currentRiseDir * Time.deltaTime * _currentRiseSpeed;
          
            _currentRiseHeight = Mathf.Max(0, _currentRiseHeight);
            targetPosition.y += _currentRiseHeight;

            // Move the resource towards the desired position
            transform.position = Vector3.MoveTowards(transform.position,
                targetPosition, normalizedSpeed * Speed * Time.deltaTime);

            if (_currentRiseHeight > MaxRiseHeight && _currentRiseDir > 0)
            {
                _currentRiseDir = -1;
            }
            // If the resource is close enough to the target, you can stop moving and perform other actions if necessary
            if (!(distance < ReachThresh) || _currentRiseDir > 0) return;
            
            if(TargetPoint.Target != null)
            {
                transform.SetParent(TargetPoint.Target);
                transform.localPosition = TargetPoint.LocalPosition;
                transform.localRotation = TargetPoint.LocalRotation;
                transform.localScale = Vector3.one;
            }
            IsMoving = false;
            ReachedTargetHandler?.Invoke();
            ReachedTargetHandler?.Invoke();
        }
    }
}