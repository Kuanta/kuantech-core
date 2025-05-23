using System;
using System.Collections.Generic;
using DG.Tweening;
using Kuantech.Utils.Math;
using UnityEngine;

namespace Kuantech.Utils
{
    public class WaypointFollower : MonoBehaviour
    {
        public class Waypoint
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public bool IsLocal;
            //public float Speed;
            public object UserData;
            
            //Event
            public Action<WaypointFollower> FollowerReachedWaypoint;
        }

        [Header("Properties")] [SerializeField]
        private GameObject ObjectToMove;
        [SerializeField] private float Speed;
        public float RotationLerpFactor = 10.0f;
        public float TargetReachThresh = 0.1f;
        public float UpdateRotationThresh = 0.1f;
        public bool ShouldUpdateRotation = true;

        [Header("Offset")] 
        public Transform AnchorPoint;

        public float SnapToPositionDelay = 0.05f;
        
        [NonSerialized] public bool Moving;
        [NonSerialized] public List<Waypoint> WaypointsList;
        [NonSerialized] public Queue<Waypoint> WaypointsQueue;
        [NonSerialized] public Waypoint CurrentWaypoint;
        private float _currentSpeed = 0;
        private int _currentSplinePointIndex;
        private Tween _moveTween;
        private Tween _rotateTween;

        
        public Action OnReachedFinalTarget;
        public Action<Waypoint> OnReachedWaypoint;

        private bool Halted = false;
        
        #region Property Setters
        public void AddWaypoint(Waypoint newWaypoint)
        {
            WaypointsQueue ??= new Queue<Waypoint>();
            WaypointsList ??= new List<Waypoint>();
            if(CurrentWaypoint == null || WaypointsQueue.Count == 0)
            {
                //No current waypoint
                CurrentWaypoint = newWaypoint;
                return;
            }
            //There is already a waypoint
            WaypointsQueue.Enqueue(newWaypoint);
            WaypointsList.Add(newWaypoint);
        }

        public void SetWaypoints(List<Waypoint> waypoints)
        {
            CurrentWaypoint = waypoints[0];
            WaypointsQueue = new Queue<Waypoint>();
            WaypointsList = waypoints;
            foreach (var waypoint in waypoints)
            {
                WaypointsQueue.Enqueue(waypoint);
            }
        }

        public void SetWaypoints(List<Vector3> points)
        {
            List<Waypoint> waypoints = new List<Waypoint>();
            foreach (var point in points)
            {
                waypoints.Add(new Waypoint()
                {
                    Position = point
                });
            }
            SetWaypoints(waypoints);
        }
        
        public void SetWaypoint(Waypoint waypoint)
        {
            WaypointsQueue = new Queue<Waypoint>();
            WaypointsList = new List<Waypoint> {waypoint};
            WaypointsQueue.Enqueue(waypoint);
            CurrentWaypoint = waypoint;
        }
        #endregion

        #region Controls
        public void SetSpeed(float speed)
        {
            Speed = speed;
        }
 
        /// <summary>
        /// For a single shot use
        /// </summary>
        /// <param name="worldPoint"></param>
        public void GoToWorldPoint(WorldPoint worldPoint, Action<WaypointFollower> onReachedAction=null)
        {
            Waypoint singleWaypoint = new Waypoint()
            {
                Position = worldPoint.GetTargetPosition(),
                Rotation = worldPoint.GetRotation(),
            };
            if (IsMoving())
            {
                WaypointsQueue.Enqueue(singleWaypoint);
                return;
            }
     
            List<Waypoint> waypoints = new List<Waypoint>();
            waypoints.Add(singleWaypoint);
            singleWaypoint.FollowerReachedWaypoint = onReachedAction;
            SetWaypoints(waypoints);
            Follow();
        }
        
        public void Follow()
        {
            KillTweens();
            Moving = true;
            _targetPosition = GetTargetPosition();
        }

        private void KillTweens()
        {
            if(_moveTween != null) _moveTween.Kill();
            _moveTween = null;

            if (_rotateTween != null) _rotateTween.Kill();
            _rotateTween = null;
        }
        
        
        public void PauseMovement()
        {
            Halted = true;
        }

        public void ResumeMovement()
        {
            Halted = false;
        }
        
        public void Stop()
        {
            Moving = false;
            KillTweens();
        }
        #endregion
        
    
        private void Update()
        {
            if (!IsMoving()) return;
            UpdatePosition();
            if(CurrentWaypoint != null) UpdateRotation(GetTargetDirection());
        }

        #region Regular Position Updates
        
        protected virtual void UpdatePosition()
        {
            if(CurrentWaypoint == null) return;
            Vector3 error = _targetPosition - GetCurrentPosition();
            float errorMag = error.sqrMagnitude;
            if(errorMag <= TargetReachThresh*TargetReachThresh)
            {
                SetNextWaypoint();
                return;
            }
            float deltaTime = Time.deltaTime;
            Vector3 direction = error.normalized;
            Vector3 moveDirection = direction;
         
            // Vector3 positionUpdate =  moveDirection * Mathf.Min(deltaTime * Speed, errorMag);
            Vector3 positionUpdate = moveDirection * deltaTime * Speed;
            SetRotation(Quaternion.LookRotation(direction));
            SetPosition(GetCurrentPosition() + positionUpdate);
        }

        private void UpdateRotation(Vector3 targetDirection)
        {
            if (CurrentWaypoint == null) return;
            if (targetDirection.sqrMagnitude <= UpdateRotationThresh * UpdateRotationThresh) return;
            SetRotation(Quaternion.Slerp(GetCurrentRotation(), Quaternion.LookRotation(targetDirection),
                Time.deltaTime * RotationLerpFactor));
        }
        
        private Vector3 _targetPosition;
        private Vector3 GetTargetPosition()
        {
            return CurrentWaypoint.IsLocal ? CurrentWaypoint.Position - transform.localPosition : CurrentWaypoint.Position;
        }

        private Vector3 _rotationTarget;
        private Vector3 GetTargetDirection()
        {
            Transform transformToMove = GetTransformToMove();
            return CurrentWaypoint.IsLocal ? CurrentWaypoint.Rotation.eulerAngles - transformToMove.localRotation.eulerAngles : CurrentWaypoint.Rotation.eulerAngles;
        }
        private void SetNextWaypoint()
        {
            WaypointsQueue ??= new Queue<Waypoint>();
            if(WaypointsQueue.Count == 0)
            {
                //Snap to position
                SnapToPosition(CurrentWaypoint.Position, CurrentWaypoint.Rotation);
                CurrentWaypoint = null;
                OnReachedFinalTarget?.Invoke();
                Moving = false;
                return;
            }
            OnReachedWaypoint?.Invoke(CurrentWaypoint); //Trigger event
            CurrentWaypoint = WaypointsQueue.Dequeue();
            _targetPosition = GetTargetPosition();
        }

        private Vector3 GetCurrentPosition()
        {
            Transform transformToMove = GetTransformToMove();
            if (AnchorPoint != null)
            {
                return transformToMove.position + (transformToMove.forward * AnchorPoint.transform.localPosition.z) ;
            }
            return transformToMove.position;
        }

        private Quaternion GetCurrentRotation()
        {
            Transform transformToMove = GetTransformToMove();
            return transformToMove.rotation;
        }

        private Transform GetTransformToMove()
        {
            if (ObjectToMove != null)
            {
                return ObjectToMove.transform;
            }

            return transform;
        }
        public void SetPosition(Vector3 position)
        {
            Transform transformToMove = GetTransformToMove();
           
            if (AnchorPoint != null)
            {
                position -= (transformToMove.forward * AnchorPoint.transform.localPosition.z) ;
            }
            transformToMove.position = position;
        }

        public void SetRotation(Quaternion rotation)
        {
            if (!ShouldUpdateRotation) return;
            Transform transformToMove = GetTransformToMove();
            transformToMove.rotation = rotation;
        }
        
        private void SnapToPosition(Vector3 position, Quaternion rotation)
        {
            Transform transformToMove = GetTransformToMove();
            if (transformToMove == null) return;
            if (SnapToPositionDelay > 0)
            {
                _moveTween = DOTween.To(GetCurrentPosition, SetPosition, position, SnapToPositionDelay).SetEase(Ease.InOutQuad).OnComplete(()=>{
                    
                });
                _rotateTween = transformToMove.DORotate(transformToMove.rotation.eulerAngles, SnapToPositionDelay).SetEase(Ease.InOutQuad);
            }
            else
            {
                SetPosition(position);
                transformToMove.rotation = rotation;
                OnReachedFinalTarget?.Invoke();
            }
        }
        #endregion
        
        public bool IsMoving()
        {
            return Moving && !Halted;
        }

        public void OnDespawn()
        {
            KillTweens();
        }
    }
}