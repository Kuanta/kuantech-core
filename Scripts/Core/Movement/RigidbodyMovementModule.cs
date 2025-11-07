using System;
using System.Collections;
using System.Collections.Generic;
using Kuantech.AI.Pathfinding;
using Kuantech.Core.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Core
{
    [RequireComponent(typeof(Rigidbody))]
    public class RigidbodyMovementModule : ActorModule
    {
        [SerializeField] private Rigidbody Rigidbody;
        [SerializeField] private float _speed;
        public Vector3 ForceMoveVector = Vector3.zero;
        private bool _movementLocked = false;
        
        //Waypoint
        private Transform _waypoint;
        private bool _goingToWaypoint;
        private UnityAction _waypointReachedHandler;
        private float _movementThreshold = 0.001f;
        
        //Movement Lock
        public LockVariable MovementLock = new LockVariable();
        
        //Dodge
        [Header("Dodge")]
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
        [Header("Jumping")]
        public LockVariable JumpLock = new LockVariable();
        public bool GroundCheckEnabled = false;
        public bool Jumping;
        public float CheckGroundedRadius = 0.2f;
        [Tooltip("Factor to multiply movement speed to add to jump force")]
        public float MovementSpeedToJumpForceFactor = 1.0f;
        public EventHandler OnJumpEvent;
        public EventHandler OnJumpLandEvent;
        public LayerMask GroundCheckMask;
        [SerializeField] private bool _isGrounded;
        private float _jumpTime;

        [Header("Path Follower")] [SerializeField]
        private PathFollower PathFollower;
        
        private float _dodgeMomentumPreserveTime = 0.5f;
        private MovementModule _movementModule;
        private AnimationModule _animationModule;
        
        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            if (Rigidbody == null)
            {
                Rigidbody = GetComponent<Rigidbody>();
            }

            _movementModule = Actor.GetModule<MovementModule>();
            _animationModule = Actor.GetModule<AnimationModule>();
        }
    
        private void FixedUpdate()
        {
            HandleMovement();
        }

        private void Update()
        {
            if (MovementLock.IsLocked())
            {
                _movementModule.SetMovementVector(Vector3.zero);
            }
            //Dodge timer
            if (_dodging && (Time.time - _dodgeStartTime) >= _dodgeDuration)
            {
                _dodging = false;
                _lastDodgeTime = Time.time;
            }
            
            HandleJumpLogic();
        }
        private void HandleMovement()
        {
            if (Actor == null) return;
            if (GameManager.IsGamePaused() || !Actor.IsAlive() || _movementModule == null)
            {
                Rigidbody.linearVelocity = Vector3.zero;
                return;
            }
            if (Rigidbody == null || Jumping) return;

            Vector3 movement = _movementModule.GetMovementVector();
            movement.y = 0;
            movement.Normalize();
            movement *= _movementModule.GetSpeed();
            float downSpeed = Rigidbody.linearVelocity.y;
            movement.y = downSpeed;
            
            Vector3 vel = movement + ForceMoveVector;

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
                Rigidbody.linearVelocity = vel;
            }
            
            
        }

        #region Queries
        
        /// <summary>
        /// Returns the movement speed
        /// </summary>
        /// <returns></returns>
        public float GetMovementSpeed()
        {
            return _speed;
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

            Vector3 movementMomentum = Rigidbody.linearVelocity;

            return movementMomentum.sqrMagnitude > dodgeMomentum.sqrMagnitude ? movementMomentum : dodgeMomentum;
        }
        
        public bool IsDodging()
        {
            return _dodging;
        }

        #endregion


        #region Controls

        public void ToggleMovement(bool toggle)
        {
            _movementLocked = !toggle;
        }

        public void SetMaxSpeed(float maxSpeed)
        {
            _speed = maxSpeed;
        }

        public void SetSpeed(float speed)
        {
            
        }

        public void Stop()
        {
            Vector3 currentRbVelocity = Rigidbody.linearVelocity;
            currentRbVelocity.x = 0;
            currentRbVelocity.z = 0;
            Rigidbody.linearVelocity = currentRbVelocity;
            _movementModule.SetMovementVector(Vector3.zero);
            ForceMoveVector = Vector3.zero;
            foreach (var routine in _knockbackRoutines)
            {
                StopCoroutine(routine);
            }
            _knockbackRoutines.Clear();
            _dodging = false;
            _dodgeSpeed = 0f;
        }

        #endregion

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
            
            if (_animationModule != null)
            {
                _animationModule.IsGroundedFlag = _isGrounded;
            }
            
            //Did we land
            if (Jumping && Time.time - _jumpTime > 0.5f && _isGrounded)
            {
                //Land
                Land();
            }
        }

        public void Jump(float jumpHeight = 1f)
        {
            if (Jumping || !_isGrounded || JumpLock.IsLocked()) return;
            
            //Check energy cost
            float energyCost = GetJumpEnergyCost();
            // if (Actor.Energy < energyCost) return;
            // Actor.SpendEnergy(energyCost);

            Vector3 currentMovementVector = _movementModule.GetMovementVector();
            Jumping = true;
            OnJumpEvent?.Invoke(this, EventArgs.Empty);
            _jumpTime = Time.time;
            float jumpForce = Mathf.Sqrt(Mathf.Abs(2 * jumpHeight * UnityEngine.Physics.gravity.y)) * Rigidbody.mass;
            Rigidbody.linearVelocity = Vector3.zero;
            Vector3 direction3d = currentMovementVector * MovementSpeedToJumpForceFactor;
            Rigidbody.AddForce(Vector3.up * jumpForce + direction3d * Rigidbody.mass, ForceMode.Impulse);

            CombatModule cm = Actor.GetModule<CombatModule>();
            if (cm != null)
            {
                cm.AttackLock.Lock(this);
            }
        }

        private void Land()
        {
            Jumping = false;
            OnJumpLandEvent?.Invoke(this, EventArgs.Empty);
            CombatModule cm = Actor.GetModule<CombatModule>();
            if (cm != null)
            {
                cm.AttackLock.Unlock(this);
            }
        }

        private bool CheckGrounded()
        {
            Vector3 center = transform.position;
            return UnityEngine.Physics.CheckSphere(center, CheckGroundedRadius, GroundCheckMask);
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