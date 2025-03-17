using Kuantech.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace Kuantech.ArcadeIdle
{
    public class ArcadeIdleMovement : ActorModule
    {
        [Header("Parameters")]
        [SerializeField] private float RotationSpeed = 10;
        [SerializeField] private float MoveVectorLerpFactor = 10f;
        [SerializeField] private Rigidbody _rigidbody;
        private ArcadeIdleAnimator _arcadeIdleAnimator;
        private Vector3 _movementVector;
        private Vector3 _currentMovementVector = Vector3.zero;

        [FormerlySerializedAs("MovementSpeedAttribute")]
        [Header("Attributes")]
        [SerializeField] private StatAttributeAsset movementSpeedAttributeAsset;


        private StatsModule _statModule;

        public override void Initialize()
        {
            base.Initialize();
            _arcadeIdleAnimator = Actor.GetModule<ArcadeIdleAnimator>();
            _statModule = Actor.GetModule<StatsModule>();
        }

        public override void Reset()
        {
        }

        private void FixedUpdate()
        {
            if (!Initialized || _statModule == null) return;
            _rigidbody.velocity = _currentMovementVector * _statModule.GetAttributeValue(movementSpeedAttributeAsset.Id);
            if(_currentMovementVector != Vector3.zero)
            {
                //Use target movement vector so that the player moves exactly where they aim for
                RotateTowardsMovementDirection(_movementVector);
            }
        }

        private void Update()
        {
            if(!Initialized || _statModule == null) return;
            _currentMovementVector =
                Vector3.Lerp(_currentMovementVector, _movementVector, Time.deltaTime * MoveVectorLerpFactor);
            if (_arcadeIdleAnimator != null) _arcadeIdleAnimator.SetSpeed(_currentMovementVector.magnitude * _statModule.GetAttributeValue(movementSpeedAttributeAsset.Id));
        }

        public void SetMovement(Vector2 movement)
        {
            if (movement.sqrMagnitude == 0.0f)
            {
                _movementVector = Vector3.zero;
                return;
            }
            movement.Normalize();
            _movementVector = GetGlobalMovementVector(movement);
        }

        public Vector3 GetGlobalMovementVector(Vector2 localMovement)
        {
            Vector3 camForward = Camera.main.transform.forward;
            camForward.y = 0;
            camForward.Normalize();
            Vector3 movement = camForward * localMovement.y + Camera.main.transform.right * localMovement.x;
            movement.y = 0;
        
            return movement.normalized;
        }
        public Vector2 GetMovement()
        {
            return _movementVector;
        }
        
        private void RotateTowardsMovementDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude <= 0) return;
            Quaternion toRotation = Quaternion.LookRotation(direction, Vector3.up);
            Quaternion rotation = Quaternion.RotateTowards(transform.rotation, toRotation, RotationSpeed * Time.deltaTime);
            transform.rotation = rotation;
        }
    }
}