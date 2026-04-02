using System;
using System.Collections;
using System.Collections.Generic;
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
        private bool _movementLocked = false;
        
        //Waypoint
        private Transform _waypoint;
        private bool _goingToWaypoint;
        private UnityAction _waypointReachedHandler;
        private float _movementThreshold = 0.001f;
        
        //Movement Lock
        
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

        [Tooltip("Factor to multiply movement speed to add to jump force")]
        public float MovementSpeedToJumpForceFactor = 1.0f;
  
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
            _movementModule.JumpHandler = HandleJump;
        }
    
        private void FixedUpdate()
        {
            HandleMovement();
        }

        private void HandleMovement()
        {
            if (Actor == null) return;
            if (GameManager.IsGamePaused() || !Actor.IsAlive() || _movementModule == null)
            {
                Rigidbody.linearVelocity = Vector3.zero;
                return;
            }
            if (Rigidbody == null || !_movementModule.IsGrounded()) return;

            Vector3 movement = _movementModule.GetMovementVector();
            movement.y = 0;
            movement.Normalize();
            movement *= _movementModule.GetSpeed();
            float downSpeed = Rigidbody.linearVelocity.y;
            movement.y = downSpeed;
            ;
            Vector3 vel = movement + Actor.MotionVectorsHandler.ForceMoveVector;
            
            if (_movementLocked)
            {
                vel = Vector3.zero;
            }
            
            if (Actor.MotionVectorsHandler.ForceMoveVector.sqrMagnitude >= 0.001f)
            {
                vel = Actor.MotionVectorsHandler.ForceMoveVector;
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

            DodgeLock.Reset();
        }
        
        # region Dodge

        public float GetDodgeEnergyCost()
        {
            return DodgeEnergyCost;
        }
        public void Dodge(Vector3 dodgeDirection, float dodgeDuration, float dodgeSpeed)
        {
            if (_dodging || DodgeLock.IsLocked()) return;
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

        
        public void HandleJump(Vector3 jumpVector)
        {
             Rigidbody.AddForce(jumpVector, ForceMode.Impulse);
        }
        #endregion
    }
}