using System;
using Kuantech.Core;
using Kuantech.Core.FX;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.ArcadeIdle
{
    public class ResourceVisual : MonoBehaviour
    {
        [NonSerialized] public string ResourceId;
        public AnimationCurve SpeedCurve;
        [NonSerialized] public WorldPoint TargetPoint;
        public float Speed = 5f;  // Speed at which the resource should fly
        public float InitialRiseSpeed = 25.0f;
        [NonSerialized] public float MaxRiseHeight = 2.0f;
        public float ReachThresh = 0.1f;

        [Header("Effects")]
        [KTTag("AudioClipTag")]
        [SerializeField] private int OnReachedAudio;
        [NonSerialized] public bool IsMoving = false;
        [NonSerialized] public UnityAction ReachedTargetHandler;
        [NonSerialized] public bool DespawnOnReach = false;

        [NonSerialized] public ResourceInventory ParentInventory;
        [NonSerialized] public ResourceDisplayer ParentDisplayer;
        
        private float _currentRiseSpeed;
        private float _currentRiseHeight;
        private int _currentRiseDir;
        public virtual void Spawn()
        {
            IsMoving = false;
            ReachedTargetHandler = null; //Reset
            DespawnOnReach = false;
        }
        
        private void Update()
        {
            if (!IsMoving) return;

            // Calculate the desired position with offset
            Vector3 targetPosition = TargetPoint.GetTargetPosition();
            float distance = Vector3.Distance(transform.position, targetPosition);
            float normalizedSpeed = SpeedCurve.Evaluate(distance);

            // Apply initial rise speed and decay it
            _currentRiseHeight += _currentRiseDir * Time.deltaTime * _currentRiseSpeed;
            if(_currentRiseHeight > MaxRiseHeight && _currentRiseDir > 0)
            {
                _currentRiseDir = -1;
            }
            _currentRiseHeight = Mathf.Max(0, _currentRiseHeight);
            targetPosition.y += _currentRiseHeight;

            // Move the resource towards the desired position
            transform.position = Vector3.MoveTowards(transform.position,
                targetPosition, normalizedSpeed * Speed * Time.deltaTime);


            // If the resource is close enough to the target, you can stop moving and perform other actions if necessary
            if (!(distance < ReachThresh) || _currentRiseDir > 0) return;
            
            if(TargetPoint.Target != null)
            {
                transform.SetParent(TargetPoint.Target);
                transform.localPosition = TargetPoint.LocalPosition;
                transform.localRotation = TargetPoint.LocalRotation;
            }
            IsMoving = false;
            ReachedTargetHandler?.Invoke();
            OnReachedTarget();
        }
        
    public void FlyToTarget(WorldPoint targetPoint)
        {
            TargetPoint = targetPoint;
            IsMoving = true;
            transform.SetParent(null, true);
            Vector3 targetPosition = TargetPoint.GetTargetPosition();
            _currentRiseSpeed = InitialRiseSpeed;
            _currentRiseHeight = 0;
            _currentRiseDir = 1;
    }

        private void OnReachedTarget()
        {
            EffectsLibrary.PlayAudio(OnReachedAudio);
            if (DespawnOnReach)
            {
                Despawn();
            }
        }

        public void Despawn()
        {
            ReachedTargetHandler = null; //Clear subscribers
            GameManager.Instance.Pool.PoolObject(gameObject);
        }
    }
}