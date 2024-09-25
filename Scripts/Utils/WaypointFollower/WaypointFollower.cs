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
        public float RotationLerpFactor = 10.0f;
        public float Speed = 10;
        public float Acceleration = 5.0f;
        public bool LockForwardMovement = false;
        public float TargetReachThresh = 0.1f;
        public float UpdateRotationThresh = 0.1f;
        public float MaxTurnAngle = 30.0f;
        public float MinTurnSpeedFactor = 0.5f;

        [Header("Spline")] public int SegmentPerSpline = 4;
        [Tooltip("If set to true, a spline will be calculated and agent will follow the spline. However this will prevent events for middle waypoints")]
        public bool UseSpline = false;
        public int SplineDegree = 5;
        public int SplineRotationLookAhead = 1;
        
        [NonSerialized] public bool Moving;
        [NonSerialized] public List<Waypoint> WaypointsList;
        [NonSerialized] public Queue<Waypoint> WaypointsQueue;
        [NonSerialized] public Waypoint CurrentWaypoint;
        private float _currentSpeed = 0;
        private Vector3[] _smoothSplinePoints = null;
        private int _currentSplinePointIndex;
        private bool _useSpline = false;
        private Tween _moveTween;
        private Tween _rotateTween;
        
        
        //Spline params
        private float _splineT;
        private float _splineTSpeed;
        private float _splineTSpeedFactor = 1;
        
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

        private float _uniformDistanceForPerSegment; //Not a very good name but its 00:33
        public void CalculateSplinePoints()
        {
            List<Vector3> points = new List<Vector3>();
            points.Add(transform.position);
            float totalDistance = 0.0f;
            foreach (var wp in WaypointsQueue)
            {
                points.Add(wp.Position);
                if (points.Count <= 1) continue;
                totalDistance += (points[^1] - points[^2]).magnitude;
            }
            _splineTSpeed = Speed/totalDistance;
            
            //_smoothSplinePoints = CatmullRomSpline.ConstructSpline(points, SegmentPerSpline).ToArray();
            int numPoints = points.Count * SegmentPerSpline;
            _smoothSplinePoints = BSpline.GenerateNURBSPath(points, SplineDegree, null, points.Count*SegmentPerSpline).ToArray();
            _uniformDistanceForPerSegment = totalDistance / (numPoints-1);
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
                _splineT = 0.0f;
                _currentSplineTSpeed = 0;
                CalculateSplinePoints();
                WaypointsQueue.Clear();
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
            if (_useSpline)
            {
                UpdatePositionForSpline();
            }
            else
            {
                UpdatePosition();
            }
        }

        protected virtual void UpdatePosition()
        {
            if(CurrentWaypoint == null && !_useSpline) return;
      
            Vector3 error = _targetPosition - transform.position;
            float errorMag = error.sqrMagnitude;
            if(errorMag <= TargetReachThresh*TargetReachThresh)
            {
                if (!_useSpline)
                {
                    OnReachedWaypoint?.Invoke(CurrentWaypoint);
                    CurrentWaypoint.FollowerReachedWaypoint?.Invoke(this);
                }
                SetNextWaypoint();
                return;
            }
            float deltaTime = Time.deltaTime;
            Vector3 direction = error.normalized;
            //float turnAngle = Vector3.Angle(transform.forward, direction);
            //float turnSpeedFactor = Mathf.Lerp(1f, MinTurnSpeedFactor, turnAngle / 90f);  // 0° = full speed, 90° or more = 50% speed
            float adjustedSpeed = Speed ;

            if (_currentSpeed < adjustedSpeed)
            {
                _currentSpeed += Acceleration * deltaTime;
            }else if (_currentSpeed > adjustedSpeed)
            {
                _currentSpeed -= Acceleration * deltaTime;
            }
            _currentSpeed  = Mathf.Clamp( _currentSpeed,  0, adjustedSpeed);

            Vector3 moveDirection = direction;
            // if (LockForwardMovement)
            // {
            //     moveDirection = transform.forward;
            // }
            Vector3 positionUpdate =  moveDirection * deltaTime * _currentSpeed;

            transform.rotation = Quaternion.LookRotation(direction);
            // if (errorMag >= UpdateRotationThresh)
            // {
            //     transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction),
            //         deltaTime * RotationLerpFactor);
            // }
            if(CurrentWaypoint.IsLocal)
            {
                transform.localPosition += positionUpdate;
            }else{
                transform.position += positionUpdate;
            }
        }

        private float _currentSplineTSpeed = 0f;
        private void UpdatePositionForSpline()
        {
            transform.position = GetSplineTargetPosition();
            Vector3 direction = _rotationTarget - transform.position;
            float turnAngle = Vector3.Angle(transform.forward, direction);
            float turnSpeedFactor = Mathf.Lerp(1f, MinTurnSpeedFactor, Mathf.Clamp(turnAngle / MaxTurnAngle, MinTurnSpeedFactor, 1));  // 0° = full speed, 90° or more = 50% speed
            UpdateRotation(_rotationTarget - transform.position);

            float targetSpeed = _splineTSpeed * _splineTSpeedFactor * turnSpeedFactor;
            if (_currentSplineTSpeed > targetSpeed)
             {
                 _currentSplineTSpeed -= Acceleration * Time.deltaTime;
             }
             else if(_currentSplineTSpeed < targetSpeed)
             {
                 _currentSplineTSpeed += Acceleration * Time.deltaTime;
            }
            _splineT += Time.deltaTime * _currentSplineTSpeed;
            if (_splineT >= 1.0f)
            {
                var lastWp = WaypointsList[^1];
                Moving = false;
                SnapToPosition(lastWp.Position, lastWp.Rotation);
                OnReachedFinalTarget?.Invoke();
            }
        }

        private void UpdateRotation(Vector3 targetDirection)
        {
            if (targetDirection.sqrMagnitude <= UpdateRotationThresh * UpdateRotationThresh) return;
            // if (LockForwardMovement)
            // {
            //     moveDirection = transform.forward;
            // }
            // Debug.LogError($"Direction: {moveDirection}");
            // Vector3 positionUpdate =  moveDirection * deltaTime * _currentSpeed;

            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetDirection),
                Time.deltaTime * RotationLerpFactor);
        }
        private Vector3 _targetPosition;
        private Vector3 GetTargetPosition()
        {
            if (!_useSpline)
            {
                return CurrentWaypoint.IsLocal ? CurrentWaypoint.Position - transform.localPosition : CurrentWaypoint.Position;
            }
            return GetSplineTargetPosition();
        }

        private Vector3 _rotationTarget;
        private Vector3 GetSplineTargetPosition()
        {
            int lengthOfArray = _smoothSplinePoints.Length;

            float currentIndex = lengthOfArray * _splineT;
            int minIndex = Mathf.Max(Mathf.FloorToInt(currentIndex), 0);
            int maxIndex = minIndex+1;
            if (minIndex >= lengthOfArray || maxIndex >= lengthOfArray)
            {
                return _smoothSplinePoints[lengthOfArray-1];
            }

            int rotationTargetIndex = maxIndex + SplineRotationLookAhead;
            rotationTargetIndex = Mathf.Min(rotationTargetIndex, lengthOfArray - 1);
            _rotationTarget = _smoothSplinePoints[rotationTargetIndex];
            Vector3 floorPos = _smoothSplinePoints[minIndex];
            Vector3 ceilPos = _smoothSplinePoints[maxIndex];
            if (maxIndex == minIndex)
            {
                _splineTSpeedFactor = 1;
            }
            else
            {
                _splineTSpeedFactor = _uniformDistanceForPerSegment/(ceilPos - floorPos).magnitude;
            }
            float innerT = currentIndex - (float)minIndex;
            return Vector3.Lerp(floorPos, ceilPos, innerT);
        }
        private void SetNextWaypoint()
        {
            _targetPosition = GetTargetPosition();
            // if (_useSpline)
            // {
            //     if (_currentSplinePointIndex >= _smoothSplinePoints.Length)
            //     {
            //         //Finished
            //         var lastWp = WaypointsList[^1];
            //         if(lastWp != null)
            //         {
            //             SnapToPosition(lastWp.Position, lastWp.Rotation);
            //         } else{
            //             OnReachedFinalTarget?.Invoke();
            //         }
            //         Moving = false;
            //     }
            //     return;
            // }
            //
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
            _moveTween = transform.DOMove(position, 0.25f).SetEase(Ease.InOutQuad).OnComplete(()=>{
                OnReachedFinalTarget?.Invoke();
            });
            _rotateTween = transform.DORotate(rotation.eulerAngles, 0.25f).SetEase(Ease.InOutQuad);

        }
        public bool IsMoving()
        {
            return CurrentWaypoint != null;
        }
    }
}