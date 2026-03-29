using System;
using DG.Tweening;
using Kuantech.Core;
using Kuantech.Core.FX;
using Kuantech.Inventory;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.ArcadeIdle
{
    //todo: Implement Throwable Mover here
    public class ResourceVisual : ItemVisual, IResourcePositioner
    {
        public AnimationCurve SpeedCurve;
        [NonSerialized] public WorldPoint TargetPoint;
        public float Speed = 5f;  // Speed at which the resource should fly
        public float InitialRiseSpeed = 25.0f;
        public float MaxRiseHeight = 3.0f;
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
        public override void Spawn(ItemData parentData)
        {
            base.Spawn(parentData);
            IsMoving = false;
            ReachedTargetHandler = null; //Reset
            DespawnOnReach = false;
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

        public void FlyToTargetWithDoJump(WorldPoint targetPoint, float jumpForce, float duration)
        {
            IsMoving = true;
            transform.DOJump(targetPoint.GetTargetPosition(), jumpForce, 1, duration, false).SetEase(Ease.Linear).OnComplete(()=>{
                IsMoving = false;
            });
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
            PoolManager.PoolObject(gameObject);
        }

        public void GoToTarget(WorldPoint targetPoint)
        {
            FlyToTarget(targetPoint);
        }

        public void WarpToPoint(WorldPoint targetPoint)
        {
            transform.SetParent(targetPoint.Target);
            transform.localPosition = targetPoint.LocalPosition;
            transform.localRotation = targetPoint.LocalRotation;
        }
    }
}