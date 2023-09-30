using System;
using System.Collections;
using System.Collections.Generic;
using Kuantech.Core.Rpg;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Core.HyperCasual
{
    public class Runner : MonoBehaviour
    {
        [Header("Speed")]
        public float BaseSpeed = 10f;
        public float SideSpeed = 10f;
        public float MovementLerpFactor = 10;
        public float SpeedLerpFactor = 1f;
        private Vector2 _movementVector = Vector2.zero;
        protected Vector2 CurrentMovementVector = Vector2.zero;
        private float _currentSpeed;
        private float _targetSpeed;

        private Vector3 ForceMovementVector;
        public Rigidbody Rigidbody;

        public bool FrontMovementBlocked = false;
        public bool ConstantForwardMovement;
        
        //Move to point
        [SerializeField] private float _targetReachThreshold = 0.05f;
        private bool _movingToPoint = false;
        private Transform _target;
        private UnityAction _pointReachedHandler;
        public LockVariable MovementLock = new LockVariable();
        public LockVariable InputLock = new LockVariable();

        private bool _pressedInput = false;

        public EventHandler InputPressedEvent;
        
        public virtual void Initialize()
        {
        }

        public void SetSpeed(float speed)
        {
            _targetSpeed = speed;
        }
        
        public Vector2 GetMovemenetVector()
        {
            return CurrentMovementVector;
        }

        #region Lifecycle

        public virtual void OnPlay()
        {
            FrontMovementBlocked = false;
            Reset();
            MovementLock.Reset();
            InputLock.Reset();
        }

        public virtual void OnMainMenu()
        {
            Reset();
        }

        public void SetInputPressed(bool toggle)
        {
            _pressedInput = toggle;
            if(toggle) InputPressedEvent?.Invoke(this, EventArgs.Empty);
        }

        public virtual void Reset()
        {
            ForceMovementVector = Vector3.zero;
            if (_knockbackRoutines != null)
            {
                foreach (var routine in _knockbackRoutines)
                {
                    StopCoroutine(routine);
                }
                _knockbackRoutines.Clear();
            }
            _pressedInput = false;
            _currentSpeed = BaseSpeed;
            _targetSpeed = BaseSpeed;
        }

        #endregion
   
        
        protected virtual void FixedUpdate()
        {
            RigibodyMovement();
        }

        protected virtual void Update()
        {
            //Check Current Level
            Level currentLevel = ((HCGameManager) HCGameManager.Instance).CurrentLevel;
            if (_movingToPoint)
            {
                MoveToTarget();
                return;
            }
            if (currentLevel == null || currentLevel.CurrentState != LevelState.Playing || MovementLock.IsLocked())
            {
                _movementVector = Vector2.zero;
                CurrentMovementVector = Vector2.zero;
                return;
            }

            CurrentMovementVector = Vector2.Lerp(CurrentMovementVector, _movementVector, MovementLerpFactor);
            ManualMovement();

            _currentSpeed = Mathf.Lerp(_currentSpeed, _targetSpeed, Time.deltaTime * SpeedLerpFactor);
        }

        public float GetForwardMovement(Vector2 movementVector)
        {
            if (HCGameManager.Instance.GameIsPaused ||
                HCGameManager.GetCurrentLevelState() != LevelState.Playing) return 0f;
            if (ConstantForwardMovement && !_pressedInput) return 0f;
            if (ConstantForwardMovement && _pressedInput) return 1f;
            return movementVector.y;
        }
        private void RigibodyMovement()
        {
            if(Rigidbody == null) return;
            if (_movingToPoint || MovementLock.IsLocked())
            {
                //Leave the movement to Update
                Rigidbody.velocity = Vector3.zero;
                return;
            }

            if (ForceMovementVector.sqrMagnitude >= 0.1f)
            {
                Rigidbody.velocity = ForceMovementVector;
                return;
            }

            Vector3 sideMovement = LocalToGlobalDirection(new Vector2(CurrentMovementVector.x, 0));
            Vector3 forwardMovement = LocalToGlobalDirection(new Vector2(0, CurrentMovementVector.y));
            Rigidbody.velocity = sideMovement * SideSpeed + forwardMovement*_currentSpeed;
        }

        private void ManualMovement()
        {
            if(Rigidbody != null) return;
            Vector3 globalDirection = LocalToGlobalDirection(CurrentMovementVector);
            transform.position += globalDirection.normalized * (Time.deltaTime * _currentSpeed);
        }

        private void MoveToTarget()
        {
            Vector3 diff = _target.position - transform.position;

            if (diff.sqrMagnitude <= _targetReachThreshold)
            {
                _movingToPoint = false;
                _pointReachedHandler?.Invoke();
                return;
            }
            diff.Normalize();
            CurrentMovementVector = new Vector2(diff.x, diff.z); //Needed for animations
            transform.position += diff * Time.deltaTime * _currentSpeed;

        }
        public void SetMovementVector(Vector2 movementVec)
        {
            if(InputLock.IsLocked())
            {
                return;
            }
            if (movementVec.sqrMagnitude > 0)
            {
                if(_pressedInput == false) InputPressedEvent?.Invoke(this, EventArgs.Empty);
                _pressedInput = true;
            }
            movementVec.y = GetForwardMovement(movementVec);
            if (_movingToPoint) return;
            _movementVector = movementVec;
            if (FrontMovementBlocked) _movementVector.y = 0;
            CurrentMovementVector = _movementVector;

        }
        public void MoveToPoint(Transform point, UnityAction pointReachedHandler)
        {
            if (_movingToPoint) return;
            Rigidbody.velocity = Vector3.zero;
            _movingToPoint = true;
            _target = point;
            _pointReachedHandler = pointReachedHandler;
        }
        public Vector3 LocalToGlobalDirection(Vector2 localDirection)
        {
            Vector3 localDirection3D = new Vector3(localDirection.x, 0f, localDirection.y);
            Vector3 globalDirection = transform.TransformDirection(localDirection3D);

            return globalDirection;
        }
        
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
            ForceMovementVector += direction;
            yield return new WaitForSeconds(knockbackTime);
            ForceMovementVector -= direction;
        }
        #endregion
    }
}