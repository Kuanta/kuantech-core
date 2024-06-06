using Kuantech.Utils;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Kuantech.AI.Utils
{
    public class NpcMovementController : MonoBehaviour
    {
        [Header("Navmesh")] 
        [SerializeField] private NavMeshAgent NavMeshAgent;
        public float MaxSpeed = 10.0f;
        public float DistanceThreshold = 0.01f;
        public float RotationThreshold = 0.1f;
        private WorldPoint _currentTarget;
        private float _remainingSqrDistanceToTarget = 0;
        private float _remainingAngleToTarget = 0;
        private bool _moving;
        private UnityAction _currentReachedHandler;


        private void Update()
        {
            if (!_moving) return;
            if (CheckReachedDestination())
            {
                OnReachedTarget();
                return;
            }
        }
        
        #region Commands
        public void SetDestintion(WorldPoint target, UnityAction reachedHandler=null)
        {
            _currentTarget = target;
            if(NavMeshAgent != null) NavMeshAgent.SetDestination(GetTargetPosition());
            CalculateRemainingDistanceAndRotation();
            _currentReachedHandler = reachedHandler;
        }
        
        /// <summary>
        /// Toggles the movement of the navmesh agent
        /// </summary>
        /// <param name="toggle"></param>
        public void ToggleMovement(bool toggle)
        {
            if(NavMeshAgent == null) return;
            NavMeshAgent.isStopped = !toggle;
            NavMeshAgent.speed = toggle ? MaxSpeed : 0f;
        }
        
        public void WarpToPoint(WorldPoint point)
        {
            if (NavMeshAgent == null)
            {
                transform.position = GetTargetPosition();
            }
            else
            {
                NavMeshAgent.Warp(point.Target != null ? point.Target.position : point.Position);
            }
            transform.rotation = point.Target != null ? point.Target.rotation : point.Rotation;
        }

        public void CalculateRemainingDistanceAndRotation()
        {   
            //Check if navmesh agent is null
            if(NavMeshAgent == null) {
                _remainingSqrDistanceToTarget = 0f;
                _remainingAngleToTarget = 0f;
                return;
            }
            _remainingSqrDistanceToTarget = Vector3.SqrMagnitude(transform.position - NavMeshAgent.destination);
            Quaternion targetRotation = GetTargetRotation();
            _remainingAngleToTarget = Quaternion.Angle(transform.rotation, targetRotation);
        }
        
        public bool CheckReachedDestination()
        {
            float distThresh = DistanceThreshold;
            if (_remainingSqrDistanceToTarget> distThresh * distThresh) return false;
            if(_remainingAngleToTarget > RotationThreshold)
            {
                return false;
            }

            transform.position = GetTargetPosition();
            transform.rotation = GetTargetRotation();
            return true;
        }

        public void OnReachedTarget()
        {
            _moving = false;
            _currentReachedHandler?.Invoke();
        }
        
        
        public void Reset()
        {
            _moving = false;
            _currentReachedHandler = null;
        }
        #endregion
        
        #region Getters

        public float GetSpeed()
        {
            return NavMeshAgent.velocity.magnitude;
        }
        
        public Vector3 GetTargetPosition()
        {
            return _currentTarget.Target != null ? _currentTarget.Target.position + _currentTarget.LocalPosition : _currentTarget.Position;
        }

        public Quaternion GetTargetRotation()
        {
            return _currentTarget.Target != null ? _currentTarget.Target.rotation : _currentTarget.Rotation;
        }
        #endregion
    }
}