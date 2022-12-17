using UnityEngine;

namespace Kuantech.Core
{
    [RequireComponent(typeof(Rigidbody))]
    public class MovementModule : Module
    {
        private Vector2 _movement;
        private float _horizontalSpeed;
        private float _verticalSpeed;

        protected override void Awake()
        {
            base.Awake();
        }
        
        private void Update()
        {
            if (Actor.Rigidbody == null) return;
            Vector3 vel = transform.right * (_horizontalSpeed * _movement.x) +
                          transform.forward * _movement.y * _verticalSpeed;
            Actor.Rigidbody.velocity = vel + Actor.ForceMoveVector;
        }
    
        public void SetMaxSpeed(float horizontalSpeed, float verticalSpeed)
        {
            _horizontalSpeed = horizontalSpeed;
            _verticalSpeed = verticalSpeed;
        }

        public void SetMovementVector(Vector2 movement)
        {
            _movement = movement;
        }
        public void Stop()
        {
            Actor.Rigidbody.velocity = Vector3.zero;
            _movement = Vector2.zero;
        }

        public override void Reset()
        {
            Stop();
        }
    }
}