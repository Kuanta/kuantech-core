using System;
using UnityEngine;

namespace Kuantech.Core
{
    [RequireComponent(typeof(Rigidbody))]
    public class MovementModule : Module
    {
        [SerializeField] private Vector2 _movement;
        [SerializeField] private float _horizontalSpeed;
        [SerializeField] private float _verticalSpeed;

        private AnimatorModule _animatorModule;
        private Vector3 _lastVelocity = Vector3.zero;
        private Vector3 _lastForcedVelocity = Vector3.zero;
        
        public override void OnModulesInitialized(object sender, EventArgs args)
        {
            _animatorModule = (AnimatorModule)Actor.GetModuleByType(typeof(AnimatorModule));
        }
        private void FixedUpdate()
        {
            if (GameManager.Instance.GameIsPaused || Actor.Health <= 0f) return;
            if (Actor.Rigidbody == null) return;
            Vector3 vel = transform.right * (_horizontalSpeed * _movement.x) +
                          transform.forward * (_movement.y * _verticalSpeed) + Actor.ForceMoveVector;

            if (Actor.ForceMoveVector.sqrMagnitude >= 0.001f)
            {
                Actor.Rigidbody.velocity = Actor.ForceMoveVector;
            }

            Actor.Rigidbody.velocity = vel;
        }

        public float GetForwardMovement()
        {
            return _movement.y;
        }

        public float GetSideMovement()
        {
            return _movement.x;
        }

        public float GetForwardSpeed()
        {
            return _verticalSpeed;
        }

        public float GetSideSpeed()
        {
            return _horizontalSpeed;
        }
        public void SetMaxSpeed(float horizontalSpeed, float verticalSpeed)
        {
            _horizontalSpeed = horizontalSpeed;
            _verticalSpeed = verticalSpeed;
        }
        
        /// <summary>
        /// Sets the global movement vector
        /// </summary>
        /// <param name="movement"></param>
        public void SetGlobalMovementVector(Vector2 movement)
        {
            Vector3 relative = transform.InverseTransformDirection(new Vector3(movement.x, 0, movement.y));
            SetMovementVector(new Vector2(relative.x, relative.z));
        }
        
        /// <summary>
        /// Sets the local movement vector. For local movement vectors, z = 1 always means forward
        /// </summary>
        /// <param name="movement"></param>
        public void SetMovementVector(Vector2 movement)
        {
            _movement = movement;
            if (_animatorModule == null) return;
            _animatorModule.SetMovementParameters(_movement);
        }
        public void Stop()
        {
            Actor.Rigidbody.velocity = Vector3.zero;
            SetMovementVector(Vector2.zero);
            _lastVelocity = Vector3.zero;
            _lastForcedVelocity = Vector3.zero;
        }

        public override void Reset()
        {
            Stop();
        }
    }
}