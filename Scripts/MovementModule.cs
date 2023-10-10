using System;
using System.Collections;
using System.Collections.Generic;
using Kuantech.Core.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Core
{
    [RequireComponent(typeof(Rigidbody))]
    public class MovementModule : Module
    {
        [SerializeField] private Vector2 _movement;
        [SerializeField] private float _horizontalSpeed;
        [SerializeField] private float _verticalSpeed;
        public Vector3 ForceMoveVector = Vector3.zero;
        private AnimatorModule _animatorModule;

        private bool _movementLocked = false;
        //Waypoint
        private Transform _waypoint;
        private bool _goingToWaypoint;
        private UnityAction _waypointReachedHandler;
        private float _movementThreshold = 0.001f;
        
        //Movement Lock
        public LockVariable MovementLock = new LockVariable();
        
        //Dodge
        public float DodgeEnergyCost;
        public LockVariable DodgeLock = new LockVariable();
        [SerializeField] private float DodgeCooldown = 0.5f;
        private bool _dodging;
        private float _dodgeStartTime;
        private float _dodgeDuration;
        private float _dodgeSpeed;
        private Vector3 _dodgeDirection;
        private float _lastDodgeTime;
        public EventHandler OnDodgeEvent;
        
        //Jumping
        public float JumpEnergyCost = 0f;
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
            HandleMovement();
        }

        private void Update()
        {
            if (_goingToWaypoint) SetWaypointMovementVectors();

            if (MovementLock.IsLocked())
            {
                SetMovementVector(new Vector2(0,0));
            }
            
            //Dodge timer
            if (_dodging && (Time.time - _dodgeStartTime) >= _dodgeDuration)
            {
                _dodging = false;
                _lastDodgeTime = Time.time;
            }
            
            HandleJumpLogic();
        }

        public bool IsDodging()
        {
            return _dodging;
        }
        
        private void HandleMovement()
        {
            if (GameManager.Instance.GameIsPaused || Actor.Health <= 0f)
            {
                Actor.Rigidbody.velocity = Vector3.zero;
                return;
            }
            if (Actor.Rigidbody == null || Jumping) return;
            
            //Rigidbody movement
            float downSpeed = Actor.Rigidbody.velocity.y;
            Vector3 vel = transform.right * (_horizontalSpeed * _movement.x) +
                          transform.forward * (_movement.y * _verticalSpeed) + ForceMoveVector;

            if (_dodging)
            {
                vel = _dodgeDirection * _dodgeSpeed + ForceMoveVector;
            }
            if (_movementLocked)
            {
                vel = Vector3.zero;
            }
            
            if (ForceMoveVector.sqrMagnitude >= 0.001f)
            {
                vel = ForceMoveVector;
            }

            vel.y = downSpeed;
            
            if (Actor.Rigidbody.isKinematic)
            {
                transform.position += vel * Time.deltaTime;
            }
            else
            {
                Actor.Rigidbody.velocity = vel;
            }
            
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
            if (_goingToWaypoint || Jumping || MovementLock.IsLocked()) return;
            Vector3 relative = transform.InverseTransformDirection(new Vector3(movement.x, 0, movement.y));
            SetMovementVector(new Vector2(relative.x, relative.z));
        }

        /// <summary>
        /// Sets the local movement vector. For local movement vectors, z = 1 always means forward
        /// </summary>
        /// <param name="movement"></param>
        /// <param name="forced">If set to true, no conditions will be checked</param>
        public void SetMovementVector(Vector2 movement, bool forced = false)
        {
            if (!forced && (_goingToWaypoint || Jumping || MovementLock.IsLocked())) return;
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
        
        public void Stop()
        {
            Vector3 currentRbVelocity = Actor.Rigidbody.velocity;
            currentRbVelocity.x = 0;
            currentRbVelocity.z = 0;
            Actor.Rigidbody.velocity = currentRbVelocity;
            SetMovementVector(Vector2.zero, forced:true);
            ForceMoveVector = Vector3.zero;
            foreach (var routine in _knockbackRoutines)
            {
                StopCoroutine(routine);
            }
            _knockbackRoutines.Clear();
            _dodging = false;
            _dodgeSpeed = 0f;
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
            MovementLock.Reset();
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
        
        # region Dodge

        public float GetDodgeEnergyCost()
        {
            return DodgeEnergyCost;
        }
        public void Dodge(Vector3 dodgeDirection, float dodgeDuration, float dodgeSpeed)
        {
            if (_dodging || Jumping || DodgeLock.IsLocked()) return;
            if (Time.time - _lastDodgeTime < DodgeCooldown) return; //Wait for cooldown
            
            //Check energy cost
            float energyCost = GetDodgeEnergyCost();
            if (Actor.Energy < energyCost) return;
            Actor.SpendEnergy(energyCost);
            _dodging = true;
            dodgeDirection.y = 0;
            dodgeDirection.Normalize();
            _dodgeDirection = dodgeDirection;
            _dodgeDuration = dodgeDuration;
            _dodgeSpeed = dodgeSpeed;
            _dodgeStartTime = Time.time;
            OnDodgeEvent?.Invoke(this, EventArgs.Empty);
        }
        #endregion
        
        #region Jumping

        private float GetJumpEnergyCost()
        {
            return 0; //Jump should be energy freee
            return JumpEnergyCost * (1 + Actor.InventoryModule.GetNormalizedEncumbrance());
        }

        private void HandleJumpLogic()
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

        public void Jump(float jumpHeight, Vector2 direction)
        {
            if (Jumping || !_isGrounded || JumpLock.IsLocked()) return;
            
            //Check energy cost
            float energyCost = GetJumpEnergyCost();
            if (Actor.Energy < energyCost) return;
            Actor.SpendEnergy(energyCost);
            
            Jumping = true;
            OnJumpEvent?.Invoke(this, EventArgs.Empty);
            _jumpTime = Time.time;
            float jumpForce = Mathf.Sqrt(Mathf.Abs(2 * jumpHeight * UnityEngine.Physics.gravity.y)) * Actor.Rigidbody.mass;
            Actor.Rigidbody.velocity = Vector3.zero;
            Vector3 direction3d = new Vector3(direction.x, 0, direction.y);
            direction3d = transform.rotation * direction3d;
            Actor.Rigidbody.AddForce(Vector3.up * jumpForce + direction3d * Actor.Rigidbody.mass, ForceMode.Impulse);
            Actor.CombatModule.AttackLock.Lock();
            Actor.CombatModule.SkillLock.Lock();
            _movement = Vector2.zero;
            if (_animatorModule == null) return;
            _animatorModule.SetMovementParameters(_movement);
        }

        private void Land()
        {
            Jumping = false;
            OnJumpLandEvent?.Invoke(this, EventArgs.Empty);
            Actor.CombatModule.AttackLock.Unlock();
            Actor.CombatModule.SkillLock.Unlock();
        }

        private bool CheckGrounded()
        {
            Vector3 center = transform.position;
            return UnityEngine.Physics.CheckSphere(center, 0.1f, GroundCheckMask);
        }
        #endregion
        
        #region Knockback
        private HashSet<IEnumerator> _knockbackRoutines = new HashSet<IEnumerator>();
        public void Knockback(Vector3 direction, float knockback, float knockbackTime)
        {
            IEnumerator routine = KnockbackRoutine(direction, knockback, knockbackTime);
            _knockbackRoutines.Add(routine);
            StartCoroutine(routine);
        }
        private IEnumerator KnockbackRoutine(Vector3 direction, float knockback, float knockbackTime)
        {
            direction.y = 0f;
            direction.Normalize();
            direction *= knockback;
            ForceMoveVector += direction;
            yield return new WaitForSeconds(knockbackTime);
            ForceMoveVector -= direction;
        }
        #endregion
    }
}