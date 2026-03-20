using System;
using System.Collections;
using Kuantech.Core.Utils;
using UnityEngine;

namespace Kuantech.Core
{
    [RequireComponent(typeof(Rigidbody))]
    public class RigidbodyMovementModule : ActorModule
    {
        [SerializeField] private Rigidbody Rigidbody;

        //Dodge
        [Header("Dodge")]
        public float DodgeEnergyCost;
        public LockVariable DodgeLock = new LockVariable();
        [SerializeField] private float DodgeCooldown = 0.5f;
        private bool _dodging;
        private float _dodgeSpeed;
        private Vector3 _dodgeDirection;
        private float _lastDodgeTime;
        public EventHandler OnDodgeEvent;

        private float _dodgeMomentumPreserveTime = 0.5f;
        private MovementModule _movementModule;
        
        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            if (Rigidbody == null)
            {
                Rigidbody = GetComponent<Rigidbody>();
            }

            _movementModule = Actor.GetModule<MovementModule>();
        }
    
        public override void ModuleFixedUpdate()
        {
            if (!Actor.IsOwner()) return;
            HandleMovement();
        }

        private void HandleMovement()
        {
            if (GameManager.IsGamePaused() || !Actor.IsAlive() || _movementModule == null)
            {
                Rigidbody.linearVelocity = Vector3.zero;
                return;
            }
            if (Rigidbody == null || !_movementModule.IsGrounded()) return;

            float downSpeed = Rigidbody.linearVelocity.y;
            Vector3 forceMove = Actor.MotionVectorsHandler.ForceMoveVector;

            Vector3 vel;
            if (forceMove.sqrMagnitude >= 0.001f)
            {
                vel = forceMove;
            }
            else if (_dodging)
            {
                vel = _dodgeDirection * _dodgeSpeed;
            }
            else if (_movementModule.IsMovementLocked())
            {
                vel = Vector3.zero;
            }
            else
            {
                Vector3 movement = _movementModule.GetMovementVector();
                movement.y = 0;
                movement.Normalize();
                movement *= _movementModule.GetSpeed();
                vel = movement;
            }

            vel.y = downSpeed;

            if (Rigidbody.isKinematic)
                transform.position += vel * Time.deltaTime;
            else
                Rigidbody.linearVelocity = vel;
        }

        #region Queries

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
            if (toggle) _movementModule.Unlock(this);
            else _movementModule.Lock(this);
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

        public override void ResetModule()
        {
            Stop();
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
            if (Time.time - _lastDodgeTime < DodgeCooldown) return;

            _dodging = true;
            _lastDodgeTime = Time.time;
            dodgeDirection.y = 0;
            dodgeDirection.Normalize();
            _dodgeDirection = dodgeDirection;
            _dodgeSpeed = dodgeSpeed;
            _movementModule.Lock(this);
            OnDodgeEvent?.Invoke(this, new DodgeEventArgs { Direction = _dodgeDirection, Duration = dodgeDuration });
            Actor.StartCoroutine(DodgeRoutine(dodgeDuration));
        }

        private IEnumerator DodgeRoutine(float duration)
        {
            yield return new WaitForSeconds(duration);
            _dodging = false;
            _dodgeSpeed = 0f;
            _movementModule.Unlock(this);
        }
        #endregion

        #region NETWORKING

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            bool isOwner = Owner.IsLocalClient;
            Rigidbody.isKinematic = !isOwner;
            Debug.Log($"[RigidbodyMovementModule] OnStartNetwork | " +
                      $"GameObject: {gameObject.name} | " +
                      $"IsOwner: {isOwner} | " +
                      $"IsServer: {IsServerStarted} | " +
                      $"IsClient: {IsClientStarted} | " +
                      $"IsKinematic: {Rigidbody.isKinematic}");
        }

#if NETWORKING_FISHNET
        public override void OnOwnershipClient(FishNet.Connection.NetworkConnection prevOwner)
        {
            base.OnOwnershipClient(prevOwner);
            Rigidbody.isKinematic = !IsOwner;
        }
#endif

        #endregion
    }
}