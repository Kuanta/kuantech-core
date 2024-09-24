using System;
using System.Collections.Generic;
using System.Linq;
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
        public float RotationLerpFactor = 10.0f;
        public float Speed = 10;
        public float Acceleration = 5.0f;
        public bool LockForwardMovement = false;
        public float TargetReachThresh = 0.1f;
        public float UpdateRotationThresh = 0.1f;

        [Header("Spline")] public int SegmentPerSpline = 4;
        [Tooltip("If set to true, a spline will be calculated and agent will follow the spline. However this will prevent events for middle waypoints")]
        public bool UseSpline = false;
        
        [NonSerialized] public bool Moving;
        [NonSerialized] public List<Waypoint> WaypointsList;
        [NonSerialized] public Queue<Waypoint> WaypointsQueue;
        [NonSerialized] public Waypoint CurrentWaypoint;
        private float _currentSpeed = 0;
        private List<Vector3> _smoothSplinePoints = new List<Vector3>();
        private int _currentSplinePointIndex;
        private bool _useSpline = false;
        private Tween _moveTween;
        private Tween _rotateTween;
        
        public Action OnReachedFinalTarget;
        public Action<Waypoint> OnReachedWaypoint;
        
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
            foreach (var wp in WaypointsQueue)
            {
                points.Add(wp.Position);
            }
            //_smoothSplinePoints = CatmullRomSpline.ConstructSpline(points, SegmentPerSpline);
            _smoothSplinePoints = BSpline.GenerateNURBSPath(points, 3, null, points.Count*4);
        }
        #endregion

        #region Controls

        public void FollowPath()
        {
            KillTweens();
            Moving = true;
            if (UseSpline && WaypointsQueue.Count > 2)
            {
                _useSpline = true;
                CalculateSplinePoints();
                _currentSplinePointIndex = 1;
            }
            else
            {
                _useSpline = false;
            }
        }

        private void KillTweens()
        {
            if(_moveTween != null) _moveTween.Kill();
            _moveTween = null;

            if (_rotateTween != null) _rotateTween.Kill();
            _rotateTween = null;
        }
        public void Stop()
        {
            Moving = false;
            KillTweens();
        }
        #endregion
        
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
            singleWaypoint.FollowerReachedWaypoint = onReachedAction;
            SetWaypoint(singleWaypoint);
            FollowPath();
        }
        private void Update()
        {
            if (!Moving) return;
            UpdatePosition();
        }

        public float MinTurnSpeedFactor = 0.5f;
        protected virtual void UpdatePosition()
        {
            if(CurrentWaypoint == null) return;
            Vector3 targetPosition = GetTargetPosition();
            Vector3 error = targetPosition - transform.position;
            float errorMag = error.magnitude;
            if(errorMag <= TargetReachThresh)
            {
                if (!_useSpline)
                {
                    OnReachedWaypoint?.Invoke(CurrentWaypoint);
                    CurrentWaypoint.FollowerReachedWaypoint?.Invoke(this);
                }
                SetNextWaypoint();
                return;
            }
            Vector3 direction = error / error.magnitude;
            float turnAngle = Vector3.Angle(transform.forward, direction);
            float turnSpeedFactor = Mathf.Lerp(1f, MinTurnSpeedFactor, turnAngle / 90f);  // 0° = full speed, 90° or more = 50% speed
            float adjustedSpeed = Speed * turnSpeedFactor;

            if (_currentSpeed < adjustedSpeed)
            {
                _currentSpeed += Acceleration * Time.deltaTime;
            }else if (_currentSpeed > adjustedSpeed)
            {
                _currentSpeed -= Acceleration * Time.deltaTime;
            }
            _currentSpeed  = Mathf.Clamp( _currentSpeed,  0, adjustedSpeed);
            
            

            Vector3 moveDirection = direction;
            if (LockForwardMovement)
            {
                moveDirection = transform.forward;
            }
            Vector3 positionUpdate =  moveDirection * Mathf.Min(errorMag, Time.deltaTime * _currentSpeed);
            
            if (direction.sqrMagnitude >= UpdateRotationThresh)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction),
                    Time.deltaTime * RotationLerpFactor);
            }
            if(CurrentWaypoint.IsLocal)
            {
                transform.localPosition += positionUpdate;
            }else{
                transform.position += positionUpdate;
            }
        }

        private Vector3 GetTargetPosition()
        {
            if (!_useSpline)
            {
                return CurrentWaypoint.IsLocal ? CurrentWaypoint.Position - transform.localPosition : CurrentWaypoint.Position;
            }

            if (_currentSplinePointIndex > _smoothSplinePoints.Count)
            {
                Debug.LogError("????");
            }
            return _smoothSplinePoints[_currentSplinePointIndex];
        }
        private void SetNextWaypoint()
        {
            if (_useSpline)
            {
                _currentSplinePointIndex++;
                if (_currentSplinePointIndex >= _smoothSplinePoints.Count)
                {
                    //Finished
                    var lastWp = WaypointsList[^1];
                    if(lastWp != null) SnapToPosition(lastWp.Position, lastWp.Rotation);
                    OnReachedFinalTarget?.Invoke();
                    Moving = false;
                }
                return;
            }
            
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
        }

        private void SnapToPosition(Vector3 position, Quaternion rotation)
        {
            _moveTween = transform.DOMove(position, 0.25f).SetEase(Ease.InOutQuad);
            _rotateTween = transform.DORotate(rotation.eulerAngles, 0.25f).SetEase(Ease.InOutQuad);
            // transform.position = position;
            // transform.rotation = rotation;
        }
        public bool IsMoving()
        {
            return CurrentWaypoint != null;
        }
        
        void OnDrawGizmosSelected()
        {
            if (_smoothSplinePoints == null || _smoothSplinePoints.Count == 0)
                return;
        
            Gizmos.color = Color.red;
            for (int i = 0; i < _smoothSplinePoints.Count - 1; i++)
            {
                Gizmos.DrawLine(_smoothSplinePoints[i], _smoothSplinePoints[i + 1]);
            }
        }
    }
}