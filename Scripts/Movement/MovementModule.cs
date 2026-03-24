using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Kuantech.Core.Utils;
using Kuantech.Rpg;
using UnityEngine;
using Attribute = Kuantech.Rpg.Attribute;

namespace Kuantech.Core
{
    public class MovementModule : ActorModule
    {
        [Header("Speed")]
        public AttributeAsset SpeedAttribute;
        [Tooltip("Fallback speed if speed attribute cant get")]
        public float Speed = 1f;
        [Tooltip("Normalized speed for animations, wont cap at max speed")]
        public float MaxSpeed;
        public float SprintMultiplier = 2;

        [Header("Crouch")] 
        public float CrouchSpeedMultiplier = 0.5f;
        public bool Crouching => _crouching;

        [Header("Dash")]
        public float DashStrength = 3f;
        public float DashDuration = 0.5f;
        [SerializeReference] public DashHandler DashHandler;
        [SerializeField] private bool LockRotationOnDash = true;
        [SerializeField] private bool SnapToDirectionOnDash = false;
        
        [SerializeReference] private CrouchHandler CrouchHandler;
        private bool _crouching;
        
        [Header("Jump")]
        public bool GroundCheckEnabled = false;
        public bool Jumping;
        public float CheckGroundedRadius = 0.2f;
        public LayerMask GroundCheckMask;
        public float JumpHeight = 2f;

        [Tooltip("A max air time to do normalization")]
        public float MaxAirTime = 5f;
        private bool _isGrounded;
        private float _jumpTime;
        private float _lastFallStartTime;
        private float _lastGroundedTime;
        private float _lastLandTime;

        //Lock
        public LockVariable MovementLock = new LockVariable();
        public LockVariable JumpLock = new LockVariable();
        
        //Events
        public EventHandler OnJumpEvent;
        public EventHandler OnJumpLandEvent;
        public EventHandler<Vector3> DashStartEvent;
        public EventHandler DashEndEvent;
        public EventHandler CrouchStarted;
        public EventHandler CrouchEnded;

        [SerializeReference] public JumpHandler JumpHandler;
        private AimHandler _aimHandler;

        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            _aimHandler = Actor.GetModule<AimHandler>();
        }
        
        public override void ModuleUpdate()
        {
            base.ModuleUpdate();
            if (!Actor.IsAlive()) return;
            HandleJumpLogic();

            if (IsGrounded())
            {
                _lastGroundedTime = Time.time;
            }
        }
        
        #region Movement Vector

        public void SetMovementVector(Vector3 movementVector)
        {
            Actor.MotionVectorsHandler.SetMovementVector(movementVector);
        }

        public Vector3 GetMovementVector()
        {
            return Actor.MotionVectorsHandler.GetMovementVector();
        }

        #endregion
        
        #region Speed
        /// <summary>
        /// Gets the actor speed
        /// </summary>
        /// <returns></returns>
        public virtual float GetSpeed()
        {
            if (IsMovementLocked()) return 0f;
            StatsModule sm = Actor.GetModule<StatsModule>();
            if (sm == null) return Speed * GetSpeedMultiplier();
            Attribute speedAtt = sm.GetAttribute(SpeedAttribute);
            if (speedAtt == null) return Speed;
            return speedAtt.GetValue(sm.GetActorLevel()) * GetSpeedMultiplier();
        }

        public float GetNormalizedSpeed()
        {
            return GetSpeed() / Mathf.Max(0.1f, MaxSpeed);
        }
        public void SetSpeed(float speed)
        {
            Speed = speed;
        }

        public void SetSprint()
        {
            SetSpeedMultiplier(SprintMultiplier);
        }

        public void StopSprint()
        {
            SetSpeedMultiplier(1);
        }
        
        public float GetSpeedMultiplier()
        {
            if (_crouching) return CrouchSpeedMultiplier;
            return Actor.MotionVectorsHandler.GetMovementMultiplier();
        }

        public void SetSpeedMultiplier(float speedMultiplier)
        {
            Actor.MotionVectorsHandler.SetMovementMultiplier(speedMultiplier);
        }
        #endregion

        #region Jump

        private readonly SyncVar<bool> _syncedJumping = new();

        private void HandleJumpLogic()
        {
            //Check for both client and server
            if (GroundCheckEnabled)
            {
                bool grounded = CheckGrounded();
                if (_isGrounded && !grounded)
                    _lastFallStartTime = Time.time;
                else if (!_isGrounded && grounded)
                    _lastLandTime = Time.time;
                _isGrounded = grounded;
            }

            // Server/owner detect landing and propagate via SyncVar
            if (Jumping && Time.time - _jumpTime > 0.5f && _isGrounded && (IsOwner || IsServerInitialized))
                Land();
        }

        public bool IsGrounded()
        {
            if (!GroundCheckEnabled) return true;
            return _isGrounded;
        }

        public void Jump()
        {
            if (Jumping || !IsGrounded() || JumpLock.IsLocked()) return;
            if (JumpHandler == null) { Debug.LogWarning("Jump handler is null"); return; }
            if (IsServerInitialized) { ExecuteJump(); _syncedJumping.Value = true; }
            else if (IsOwner)        { ExecuteJump(); ServerRpc_Jump(); }
        }

        [ServerRpc]
        private void ServerRpc_Jump() { ExecuteJump(); _syncedJumping.Value = true; }

        private void OnSyncedJumpingChanged(bool _, bool next, bool asServer)
        {
            if (asServer || IsOwner) return;
            // Observer: update state and fire events for animation — no physics
            if (next)
            {
                Jumping = true;
                _jumpTime = Time.time;
                OnJumpEvent?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ExecuteLand();
            }
        }

        private void ExecuteJump()
        {
            Vector3 jumpVector = GetJumpVector();
            Jumping = true;
            _jumpTime = Time.time;
            JumpHandler?.HandleJump(this, jumpVector);
            OnJumpEvent?.Invoke(this, EventArgs.Empty);
        }

        private void Land()
        {
            ExecuteLand();
            if (IsServerInitialized) _syncedJumping.Value = false;
        }

        private void ExecuteLand()
        {
            Jumping = false;
            OnJumpLandEvent?.Invoke(this, EventArgs.Empty);
        }

        public Vector3 GetJumpVector()
        {
            float jumpForce = Mathf.Sqrt(Mathf.Abs(2 * JumpHeight * UnityEngine.Physics.gravity.y));
            return GetMovementVector() * GetSpeed() + Vector3.up * jumpForce;
        }

        public float GetAirTime()
        {
            if (!IsGrounded()) return Time.time - _lastGroundedTime;
            return _lastLandTime - _lastFallStartTime;
        }

        public float GetNormalizedAirTime()
        {
            return Mathf.Clamp01(GetAirTime() / Mathf.Max(MaxAirTime, 0.1f));
        }

        private bool CheckGrounded()
        {
            return UnityEngine.Physics.CheckSphere(transform.position, CheckGroundedRadius, GroundCheckMask);
        }

        #endregion
        
        #region Locks

        public bool IsMovementLocked()
        {
            if (MovementLock != null && MovementLock.IsLocked()) return true;
            return false;
        }
        
        /// <summary>
        /// Locks the movement
        /// </summary>
        /// <param name="locker"></param>
        public void Lock(object locker)
        {
            if (MovementLock == null)
            {
                MovementLock = new LockVariable();
            }
            MovementLock.Lock(locker);
            if (MovementLock.IsLocked())
            {
                MovementLocked();
            }
        }
        
        /// <summary>
        /// Unlocks the movement
        /// </summary>
        /// <param name="locker"></param>
        public void Unlock(object locker)
        {
            if (!IsMovementLocked()) return;
            bool alreadyUnlocked = !MovementLock.IsLocked();
            if (alreadyUnlocked) return;
            
            MovementLock.Unlock(locker);
            if(!MovementLock.IsLocked())
                MovementUnlocked();
            else
                MovementLocked();
        }
        protected virtual void MovementLocked()
        {
            
        }
        
        protected virtual void MovementUnlocked()
        {
            
        }
        #endregion

        #region Crouch

        private readonly SyncVar<bool> _syncedCrouching = new();

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _syncedCrouching.OnChange += OnSyncedCrouchingChanged;
            _syncedJumping.OnChange += OnSyncedJumpingChanged;
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            _syncedCrouching.OnChange -= OnSyncedCrouchingChanged;
            _syncedJumping.OnChange -= OnSyncedJumpingChanged;
        }

        public void Crouch()
        {
            if (IsServerInitialized) { ExecuteCrouch(); _syncedCrouching.Value = true; }
            else if (IsOwner)        { ExecuteCrouch(); ServerRpc_Crouch(); }
        }

        public void Standup()
        {
            if (IsServerInitialized) { ExecuteStandup(); _syncedCrouching.Value = false; }
            else if (IsOwner)        { ExecuteStandup(); ServerRpc_Standup(); }
        }

        [ServerRpc]
        private void ServerRpc_Crouch()
        {
            ExecuteCrouch();
            _syncedCrouching.Value = true;
        }

        [ServerRpc]
        private void ServerRpc_Standup()
        {
            ExecuteStandup();
            _syncedCrouching.Value = false;
        }

        private void OnSyncedCrouchingChanged(bool _, bool next, bool asServer)
        {
            if (asServer || IsOwner) return;
            if (next) ExecuteCrouch(); else ExecuteStandup();
        }

        private void ExecuteCrouch()
        {
            if (CrouchHandler == null) return;
            CrouchHandler.OnCrouchStarted();
            CrouchStarted?.Invoke(this, EventArgs.Empty);
            _crouching = true;
        }

        private void ExecuteStandup()
        {
            if (CrouchHandler == null) return;
            CrouchHandler.OnCrouchEnd();
            CrouchEnded?.Invoke(this, EventArgs.Empty);
            _crouching = false;
        }

        #endregion

        #region Dash

        public void Dash(Vector3 direction)
        {
            if (IsServerInitialized)
            {
                ExecuteDash(direction);
                if (IsSpawned) ObserversRpc_Dash(direction);
            }
            else if (IsOwner)
            {
                ExecuteDash(direction);
                ServerRpc_Dash(direction);
            }
        }

        [ServerRpc]
        private void ServerRpc_Dash(Vector3 direction)
        {
            ExecuteDash(direction); // physics only — no OnDashEvent
            ObserversRpc_Dash(direction);
        }

        // Fires on all observers (incl. listen-server host when not owner). Owner already fired locally.
        [ObserversRpc(ExcludeOwner = true)]
        private void ObserversRpc_Dash(Vector3 direction) { 
            ExecuteDash(direction);
        }

        private void ExecuteDash(Vector3 direction)
        {
            DashHandler?.OnDashStart(this, direction);
            DashStartEvent?.Invoke(this, direction);
            if (LockRotationOnDash && _aimHandler != null)
            {
                if (SnapToDirectionOnDash) _aimHandler.SetDirection(direction);
                _aimHandler.LockRotation(this);
            }
            DashHandler?.HandleDash(this, direction);
            // OnDashEvent intentionally NOT fired here — caller is responsible
            StartCoroutine(DashEndRoutine());
        }

        private IEnumerator DashEndRoutine()
        {
            yield return new WaitForSeconds(DashDuration);
            DashHandler?.OnDashEnd(this);
            DashEndEvent?.Invoke(this, EventArgs.Empty);
            if (_aimHandler != null) _aimHandler.UnlockRotation(this);
        }

        #endregion
        #region Knockback
        private HashSet<IEnumerator> _knockbackRoutines = new HashSet<IEnumerator>();
        
        public void Knockback(Vector3 direction, float knockback, float knockbackTime)
        {
            if (knockback == 0 || knockbackTime == 0) return;
            IEnumerator routine = KnockbackRoutine(direction, knockback, knockbackTime);
            _knockbackRoutines.Add(routine);
            StartCoroutine(routine);
        }
        
        private IEnumerator KnockbackRoutine(Vector3 direction, float knockback, float knockbackTime)
        {
            direction.Normalize();
            direction *= knockback;
            AddForceMoveVector(direction);
            yield return new WaitForSeconds(knockbackTime);
            RemoveForceMoveVector(direction);
        }
        
        protected virtual void AddForceMoveVector(Vector3 forceMoveVector)
        {
            Actor.MotionVectorsHandler.AddForceMovementVector(forceMoveVector);
        }
        
        protected virtual void RemoveForceMoveVector(Vector3 forceMoveVector)
        {
            Actor.MotionVectorsHandler.RemoveForceMovementVector(forceMoveVector);
        }

        public Vector3 GetKnockbackVector()
        {
            return Actor.MotionVectorsHandler.ForceMoveVector;
        }
        #endregion

        public void Stop()
        {
            SetMovementVector(Vector3.zero);
        }
        
        public override void ResetModule()
        {
            base.ResetModule();
            MovementLock.Reset();

            Stop();
            //Zero out the knockback vectors
            foreach (var routine in _knockbackRoutines)
            {
                StopCoroutine(routine);
            }
            _knockbackRoutines.Clear();
            Actor.MotionVectorsHandler.ForceMoveVector = Vector3.zero;
            
            SetSpeedMultiplier(1);
            
            //Reset jump
            Jumping = false;
            if (IsServerInitialized) _syncedJumping.Value = false;
            JumpLock.Reset();
            _lastGroundedTime = Time.time;
            _crouching = false;
            if (CrouchHandler != null)
            {
                CrouchHandler.Reset();
            }
        }
    }
}