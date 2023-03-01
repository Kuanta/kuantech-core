using System;
using Kuantech.Core.Rpg;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

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

        private bool _movementLocked = false;
        //Waypoint
        private Transform _waypoint;
        private bool _goingToWaypoint;
        private UnityAction _waypointReachedHandler;
        private float _movementThreshold = 0.001f;
        
        //Dodge
        public LockVariable DodgeLock = new LockVariable();
        [SerializeField] private float DodgeCooldown = 0.5f;
        private bool _dodging;
        private float _dodgeStartTime;
        private float _dodgeDuration;
        private float _dodgeSpeed;
        private Vector3 _dodgeDirection;
        private float _lastDodgeTime;
        
        //Jumping
        public LockVariable JumpLock = new LockVariable();
        public bool GroundCheckEnabled = false;
        public bool Jumping;
        public EventHandler OnJumpEvent;
        public EventHandler OnJumpLandEvent;
        public LayerMask GroundCheckMask;
        [SerializeField] private bool _isGrounded;
        private float _jumpTime;

        private Vector3 _momentumVector;
        private float _dodgeMomentumPreserveTime = 0.5f;
        
        public override void OnModulesInitialized(object sender, EventArgs args)
        {
            _animatorModule = (AnimatorModule)Actor.GetModuleByType(typeof(AnimatorModule));
        }
        private void FixedUpdate()
        {
            if (GameManager.Instance.GameIsPaused || Actor.Health <= 0f)
            {
                Actor.Rigidbody.velocity = Vector3.zero;
                return;
            }
            if (Actor.Rigidbody == null) return;
            Vector3 vel = transform.right * (_horizontalSpeed * _movement.x) +
                          transform.forward * (_movement.y * _verticalSpeed) + Actor.ForceMoveVector;

            if (_dodging)
            {
                vel = _dodgeDirection * _dodgeSpeed + Actor.ForceMoveVector;
            }
            if (_movementLocked)
            {
                vel = Vector3.zero;
            }
            
            if (Actor.ForceMoveVector.sqrMagnitude >= 0.001f)
            {
                vel = Actor.ForceMoveVector;
            }

            vel.y = Actor.Rigidbody.velocity.y;
            Actor.Rigidbody.velocity = vel;
        }

        private void Update()
        {
            if (_goingToWaypoint) SetWaypointMovementVectors();
            
            //Dodge timer
            if (_dodging && (Time.time - _dodgeStartTime) >= _dodgeDuration)
            {
                _dodging = false;
                _lastDodgeTime = Time.time;
            }
            
            HandleJumpLogic();
            
        }

        public Vector3 GetMomentumVector()
        {
            Vector3 dodgeMomentum = Vector3.zero;
            if (Time.time - _lastDodgeTime < _dodgeMomentumPreserveTime)
            {
                dodgeMomentum = _dodgeDirection * _dodgeSpeed;
            }

            Vector3 movementMomentum = Actor.Rigidbody.velocity;

            return movementMomentum.sqrMagnitude > dodgeMomentum.sqrMagnitude ? movementMomentum : dodgeMomentum;
        }
        public void ToggleMovement(bool toggle)
        {
            _movementLocked = !toggle;
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
            return _verticalSpeed * GetForwardMovement();
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
            if (_goingToWaypoint || Jumping) return;
            Vector3 relative = transform.InverseTransformDirection(new Vector3(movement.x, 0, movement.y));
            SetMovementVector(new Vector2(relative.x, relative.z));
        }
        
        /// <summary>
        /// Sets the local movement vector. For local movement vectors, z = 1 always means forward
        /// </summary>
        /// <param name="movement"></param>
        public void SetMovementVector(Vector2 movement)
        {
            if (_goingToWaypoint || Jumping) return;
            _movement = movement;
            if (_animatorModule == null) return;
            _animatorModule.SetMovementParameters(_movement);
        }

        public Vector2 GetLocalMovementVector()
        {
            return _movement;
        }

        public Vector3 GetGlobalMovementVector()
        {
            Vector3 upVector = new Vector3(_movement.x * GetSideSpeed(), 0, _movement.y * GetForwardSpeed());
            return Quaternion.AngleAxis(transform.rotation.eulerAngles.y, Vector3.up) * upVector;
        }
        
        public void Dodge(Vector3 dodgeDirection, float dodgeDuration, float dodgeSpeed)
        {
            if (_dodging || Jumping || DodgeLock.IsLocked()) return;
            if (Time.time - _lastDodgeTime < DodgeCooldown) return; //Wait for cooldown
            _dodging = true;
            dodgeDirection.y = 0;
            dodgeDirection.Normalize();
            _dodgeDirection = dodgeDirection;
            _dodgeDuration = dodgeDuration;
            _dodgeSpeed = dodgeSpeed;
            _dodgeStartTime = Time.time;
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
            _movementLocked = false;
            _goingToWaypoint = false;
            _dodging = false;
            _lastDodgeTime = 0;
            Jumping = false;
            JumpLock.Reset();
            DodgeLock.Reset();
        }

        #region Waypoint following
        public void GoToWaypoint(Transform point, UnityAction handler, float threshold = 0.01f)
        {
            Stop();
            _waypoint = point;
            _goingToWaypoint = true;
            _waypointReachedHandler = handler;
            _movementThreshold = threshold;
        }

        private void SetWaypointMovementVectors()
        {
            Vector3 diffVec = _waypoint.position - transform.position;
            if (diffVec.sqrMagnitude <= _movementThreshold * _movementThreshold)
            {
                _goingToWaypoint = false;
                _waypoint = null;
                _waypointReachedHandler?.Invoke();
                _waypointReachedHandler = null;
                Stop();
                return;
            }

            diffVec.y = 0;
            diffVec.Normalize();
            Vector3 relative = transform.InverseTransformDirection(diffVec);
            _movement = new Vector2(relative.x, relative.z);
            if (_animatorModule == null) return;
            _animatorModule.SetMovementParameters(_movement);

        }
        #endregion

        #region Jumping

        public void HandleJumpLogic()
        {
            if (!GroundCheckEnabled) return;
            _isGrounded = CheckGrounded();
            
            //Did we land
            if (Jumping && Time.time - _jumpTime > 0.5f && _isGrounded)
            {
                //Land
                Land();
            }
        }
        public void Jump(float jumpHeight)
        {
            if (Jumping || !_isGrounded || JumpLock.IsLocked()) return;
            Jumping = true;
            OnJumpEvent?.Invoke(this, EventArgs.Empty);
            _jumpTime = Time.time;
            float jumpForce = Mathf.Sqrt(Mathf.Abs(2 * jumpHeight * UnityEngine.Physics.gravity.y)) * Actor.Rigidbody.mass;
            Actor.Rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        private void Land()
        {
            Jumping = false;
            OnJumpLandEvent?.Invoke(this, EventArgs.Empty);
        }

        private bool CheckGrounded()
        {
            Vector3 center = transform.position;
            return UnityEngine.Physics.CheckSphere(center, 0.1f, GroundCheckMask);
        }
        #endregion
    }
}