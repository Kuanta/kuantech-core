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

        [Header("Properties")] 
        [SerializeField] private float Speed;
        public float RotationLerpFactor = 10.0f;
        public float TargetReachThresh = 0.1f;
        public float UpdateRotationThresh = 0.1f;
        public bool ShouldUpdateRotation = true;

        [Header("Spline")]
        public SplineFollower SplineFollower;
        
        [Tooltip("If set to true, a spline will be calculated and agent will follow the spline. However this will prevent events for middle waypoints")]
        public bool UseSpline = false;

        [Header("Offset")] 
        public Transform AnchorPoint;

        public float SnapToPositionDelay = 0.05f;
        
        [NonSerialized] public bool Moving;
        [NonSerialized] public List<Waypoint> WaypointsList;
        [NonSerialized] public Queue<Waypoint> WaypointsQueue;
        [NonSerialized] public Waypoint CurrentWaypoint;
        private float _currentSpeed = 0;
        private int _currentSplinePointIndex;
        private bool _useSpline = false;
        private Tween _moveTween;
        private Tween _rotateTween;

        
        public Action OnReachedFinalTarget;
        public Action<Waypoint> OnReachedWaypoint;

        private bool Halted = false;
        
        #region Property Setters
        public void AddWaypoint(Waypoint newWaypoint)
        {
            if(WaypointsQueue == null) WaypointsQueue = new Queue<Waypoint>();
            if (WaypointsList == null) WaypointsList = new List<Waypoint>();
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

        public void SetWaypoint(Waypoint waypoint)
        {
            WaypointsQueue = new Queue<Waypoint>();
            WaypointsList = new List<Waypoint>();
            WaypointsList.Add(waypoint);
            WaypointsQueue.Enqueue(waypoint);
            CurrentWaypoint = waypoint;
        }

        public void CalculateSplinePoints()
        {
            List<Vector3> points = new List<Vector3>();
            points.Add(transform.position);
            foreach (var wp in WaypointsQueue)
            {
                points.Add(wp.Position);
                if (points.Count <= 1) continue;
            }
            SplineFollower.SetSpline(points);
        }
        #endregion

        #region Controls
        public void SetSpeed(float speed)
        {
            Speed = speed;
        }
        public void SetSplineSpeed(float splineSpeed)
        {
            if (SplineFollower != null)
            {
                SplineFollower.FollowSpeed = splineSpeed;
            }
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
            if (IsMoving() && !_useSpline)
            {
                WaypointsQueue.Enqueue(singleWaypoint);
                return;
            }
            
            // Waypoint currentWaypoint = new Waypoint()
            // {
            //     Position = transform.position,
            //     Rotation = transform.rotation,
            // };
            List<Waypoint> waypoints = new List<Waypoint>();
            waypoints.Add(singleWaypoint);
            singleWaypoint.FollowerReachedWaypoint = onReachedAction;
            SetWaypoints(waypoints);
            FollowPath();
        }
        
        public void FollowPath()
        {
            KillTweens();
            Moving = true;
            if (UseSpline && WaypointsQueue.Count >= 2 && SplineFollower != null)
            {
                _useSpline = true;
                CalculateSplinePoints();
                WaypointsQueue.Clear();
                SplineFollower.OnReachedTarget = () =>
                {
                    OnReachedFinalTarget?.Invoke();
                };
                SplineFollower.FollowSpline();
            }
            else
            {
                _useSpline = false;
            }
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
            if (SplineFollower != null)
            {
                SplineFollower.Halt();
            }
        }

        public void ResumeMovement()
        {
            Halted = false;
            if (SplineFollower != null)
            {
                SplineFollower.Resume();
            }
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
            if (!_useSpline)
            {
                UpdatePosition();
            }
            
            //Spline update is done by the spline follower
        }

        #region Regular Position Updates

        
        protected virtual void UpdatePosition()
        {
            if(CurrentWaypoint == null && !_useSpline) return;
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
         
            Vector3 positionUpdate =  moveDirection * Mathf.Min(deltaTime * Speed, errorMag);
            SetRotation(Quaternion.LookRotation(direction));
            SetPosition(GetCurrentPosition() + positionUpdate);
        }



        private void UpdateRotation(Vector3 targetDirection)
        {
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
            CurrentWaypoint = WaypointsQueue.Dequeue();
            _targetPosition = GetTargetPosition();
        }

        private Vector3 GetCurrentPosition()
        {
            if (AnchorPoint != null)
            {
                return transform.position + (transform.forward * AnchorPoint.transform.localPosition.z) ;
            }
            return transform.position;
        }

        private Quaternion GetCurrentRotation()
        {
            return transform.rotation;
        }

        public void SetPosition(Vector3 position)
        {
            if (transform == null) return;
            if (AnchorPoint != null)
            {
                position = position - (transform.forward * AnchorPoint.transform.localPosition.z) ;
            }
            transform.position = position;
        }

        public void SetRotation(Quaternion rotation)
        {
            if (!ShouldUpdateRotation) return;
            transform.rotation = rotation;
        }
        
        private void SnapToPosition(Vector3 position, Quaternion rotation)
        {
            if (gameObject == null || transform == null) return;
            if (SnapToPositionDelay > 0)
            {
                _moveTween = DOTween.To(GetCurrentPosition, SetPosition, position, SnapToPositionDelay).SetEase(Ease.InOutQuad).OnComplete(()=>{
                    
                });
                _rotateTween = transform.DORotate(rotation.eulerAngles, SnapToPositionDelay).SetEase(Ease.InOutQuad);
            }
            else
            {
                SetPosition(position);
                transform.rotation = rotation;
                OnReachedFinalTarget?.Invoke();
            }

        }

        #endregion
        
        public bool IsMoving()
        {
            if (_useSpline) return SplineFollower.IsMoving();
            return Moving && !Halted;
        }

        public void OnDespawn()
        {
            KillTweens();
        }
    }
}