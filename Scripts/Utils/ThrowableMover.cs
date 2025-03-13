using System;
using Kuantech.Core;
using Kuantech.Core.FX;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Utils
{
    /// <summary>
    /// A component that moves the object in a "throwable" fashion
    /// </summary>
    public class ThrowableMover : MonoBehaviour
    {
        [Header("Properties")]
        public float Speed = 5f;  // Speed at which the resource should fly
        public float InitialRiseSpeed = 25.0f;
        public float MaxRiseHeight = 3.0f;
        public float ReachThresh = 0.1f;
        public AnimationCurve SpeedCurve;
        public bool UsePool = false;
        public bool DespawnOnReach = false;

        [Header("Effects")] 
        [SerializeField] private EffectPlayer OnReachedEffect;
        
        private float _currentRiseSpeed;
        private float _currentRiseHeight;
        private int _currentRiseDir;
        [NonSerialized] public WorldPoint TargetPoint;
        [NonSerialized] public bool IsMoving = false;
        [NonSerialized] public UnityAction ReachedTargetHandler;

        public void ThrowToTarget(WorldPoint target, UnityAction reachedTargetHandler = null)
        {
            TargetPoint = target;
            IsMoving = true;
            transform.SetParent(null, true);
            _currentRiseSpeed = InitialRiseSpeed;
            _currentRiseHeight = 0;
            _currentRiseDir = 1;
            ReachedTargetHandler = reachedTargetHandler;
        }
        
        private void Update()
        {
            if (!IsMoving || TargetPoint == null) return;
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
            }
            IsMoving = false;
            OnReachedTarget();
        }
        
        protected virtual void OnReachedTarget()
        {
            ReachedTargetHandler?.Invoke();
            OnReachedEffect.PlayEffectAtPosition(transform.position, transform.rotation);
            if (DespawnOnReach)
            {
                Despawn();
            }
        }

        private void Despawn()
        {
            ReachedTargetHandler = null; //Clear subscribers
            if (UsePool)
            {
                PoolManager.PoolObject(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}