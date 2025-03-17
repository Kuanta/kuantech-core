using System;
using System.Collections;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.Combat;
using Kuantech.Core.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Core
{
    [RequireComponent(typeof(Rigidbody))]
    public class MovementModule : ActorModule
    {
        [SerializeField] private Rigidbody Rigidbody;
        [SerializeField] private Vector2 _movement;
        [SerializeField] private float _speed;
        public Vector3 ForceMoveVector = Vector3.zero;
        private AnimationModule _animationModule;

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
        
        public override void OnModulesInitialized()
        {
            _animationModule = Actor.GetModule<AnimationModule>();
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
            if (Actor == null) return;
            if (GameManager.IsGamePaused() || !Actor.IsAlive())
            {
                Rigidbody.velocity = Vector3.zero;
                return;
            }
            if (Rigidbody == null || Jumping) return;
            
            //Rigidbody movement
            float downSpeed = Rigidbody.velocity.y;
            Vector3 vel = transform.right * (_speed * _movement.x) +
                          transform.forward * (_movement.y * _speed) + ForceMoveVector;

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
            
            if (Rigidbody.isKinematic)
            {
                transform.position += vel * Time.deltaTime;
            }
            else
            {
                Rigidbody.velocity = vel;
            }
            
        }
        
        /// <summary>
        /// Returns the vector of the forward direction of the actor
        /// </summary>
        /// <returns></returns>
        public Vector3 GetForwardVector()
        {
            //todo: Implement aim mechanics
            return transform.forward;
        }
        
        public Vector3 GetMomentumVector()
        {
            Vector3 dodgeMomentum = Vector3.zero;
            if (Time.time - _lastDodgeTime < _dodgeMomentumPreserveTime)
            {
                dodgeMomentum = _dodgeDirection * _dodgeSpeed;
            }

            Vector3 movementMomentum = Rigidbody.velocity;

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

     

        public float GetSpeed()
        {
            return _speed;
        }
        public void SetMaxSpeed(float maxSpeed)
        {
            _speed = maxSpeed;
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
            if (_animationModule == null) return;
            _animationModule.SetMovementParameters(_movement);
        }

        public void Stop()
        {
            Vector3 currentRbVelocity = Rigidbody.velocity;
            currentRbVelocity.x = 0;
            currentRbVelocity.z = 0;
            Rigidbody.velocity = currentRbVelocity;
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
            if (_animationModule == null) return;
            _animationModule.SetMovementParameters(_movement);

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
            // if (Actor.Energy < energyCost) return;
            // Actor.SpendEnergy(energyCost);
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
            // if (Actor.Energy < energyCost) return;
            // Actor.SpendEnergy(energyCost);
            
            Jumping = true;
            OnJumpEvent?.Invoke(this, EventArgs.Empty);
            _jumpTime = Time.time;
            float jumpForce = Mathf.Sqrt(Mathf.Abs(2 * jumpHeight * UnityEngine.Physics.gravity.y)) * Rigidbody.mass;
            Rigidbody.velocity = Vector3.zero;
            Vector3 direction3d = new Vector3(direction.x, 0, direction.y);
            direction3d = transform.rotation * direction3d;
            Rigidbody.AddForce(Vector3.up * jumpForce + direction3d * Rigidbody.mass, ForceMode.Impulse);

            CombatModule cm = Actor.GetModule<CombatModule>();
            if (cm != null)
            {
                cm.AttackLock.Lock();
                cm.SkillLock.Lock();
            }
       
            _movement = Vector2.zero;
            if (_animationModule == null) return;
            _animationModule.SetMovementParameters(_movement);
        }

        private void Land()
        {
            Jumping = false;
            OnJumpLandEvent?.Invoke(this, EventArgs.Empty);
            CombatModule cm = Actor.GetModule<CombatModule>();
            if (cm != null)
            {
                cm.AttackLock.Unlock();
                cm.SkillLock.Unlock();
            }
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