using Kuantech.Core.Rpg;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Core.HyperCasual
{
    public class Runner : MonoBehaviour
    {
        public float Speed = 10f;
        private Vector2 _movementVector = Vector2.zero;
        public Rigidbody Rigidbody;

        public bool FrontMovementBlocked = false;
        
        //Move to point
        [SerializeField] private float _targetReachThreshold = 0.05f;
        private bool _movingToPoint = false;
        private Transform _target;
        private UnityAction _pointReachedHandler;
        public LockVariable MovementLock = new LockVariable();
        
        public void Initialize()
        {
        }

        public Vector2 GetMovemenetVector()
        {
            return _movementVector;
        }

        public void OnPlay()
        {
            FrontMovementBlocked = false;
            MovementLock.Reset();
        }
        
        private void FixedUpdate()
        {
            RigibodyMovement();
        }

        private void Update()
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
                return;
            }
            ManualMovement();
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
            Vector3 globalDirection = LocalToGlobalDirection(_movementVector);
            globalDirection.y = Rigidbody.velocity.y;
            Rigidbody.velocity = globalDirection * Speed;
        }

        private void ManualMovement()
        {
            if(Rigidbody != null) return;
            Vector3 globalDirection = LocalToGlobalDirection(_movementVector);
            transform.position += globalDirection.normalized * (Time.deltaTime * Speed);
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
            _movementVector = new Vector2(diff.x, diff.z); //Needed for animations
            transform.position += diff * Time.deltaTime * Speed;

        }
        public void SetMovementVector(Vector2 movementVec)
        {
            if (_movingToPoint) return;
            _movementVector = movementVec;
            if (FrontMovementBlocked) _movementVector.y = 0;
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
        
    }
}