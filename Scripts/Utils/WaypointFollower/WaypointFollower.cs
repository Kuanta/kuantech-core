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
        public float Speed;
        public float RotationLerpFactor = 10.0f;
        public float Acceleration = 5.0f;
        public float TargetReachThresh = 0.1f;
        public float UpdateRotationThresh = 0.1f;
        public float MaxTurnAngle = 30.0f;
        public float MinTurnSpeedFactor = 0.5f;

        [Header("Spline")] public int SegmentPerSpline = 4;
        [Tooltip("If set to true, a spline will be calculated and agent will follow the spline. However this will prevent events for middle waypoints")]
        public bool UseSpline = false;
        public int SplineDegree = 5;
        public int SplineRotationLookAhead = 1;
        public float SplineSpeed = 5;

        [Header("Offset")] 
        public Transform AnchorPoint;
        
        [NonSerialized] public bool Moving;
        [NonSerialized] public List<Waypoint> WaypointsList;
        [NonSerialized] public Queue<Waypoint> WaypointsQueue;
        [NonSerialized] public Waypoint CurrentWaypoint;
        private float _currentSpeed = 0;
        private Vector3[] _smoothSplinePoints = null;
        private float[] _arcLengths = null;
        private float[] _normalizedArcLengths = null;
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

        public bool Halted = false;
        
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
            _splineTSpeed = SplineSpeed;
            //_smoothSplinePoints = CatmullRomSpline.ConstructSpline(points, SegmentPerSpline).ToArray();
            _smoothSplinePoints = BSpline.GenerateNURBSPath(points, SplineDegree, null, points.Count*SegmentPerSpline).ToArray();
            ComputeArcLengths();
        }

        private void ComputeArcLengths()
        {
            int lengthOfArray = _smoothSplinePoints.Length;
            _arcLengths = new float[lengthOfArray];
            _normalizedArcLengths = new float[lengthOfArray];
            _arcLengths[0] = 0.0f;
            for (int i = 1; i < lengthOfArray; i++)
            {
                float segmentLength = Vector3.Distance(_smoothSplinePoints[i - 1], _smoothSplinePoints[i]);
                _arcLengths[i] = _arcLengths[i - 1] + segmentLength;  // Cumulative distance
            }

            float totalLength = GetArcTotalLength();
            for (int i = 0; i < _arcLengths.Length; ++i)
            {
                _normalizedArcLengths[i] = _arcLengths[i] / totalLength;
            }
        }

        private float GetArcTotalLength()
        {
            return _arcLengths[^1];
        }
        #endregion

        #region Controls

        public void FollowPath()
        {
            KillTweens();
            Moving = true;
            if (UseSpline && WaypointsQueue.Count >= 2)
            {
                _useSpline = true;
                _splineT = 0.0f;
                _currentSplineTSpeed = _splineTSpeed * _splineTSpeedFactor;
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
            if (!Moving || Halted) return;
            if (_useSpline)
            {
                UpdatePositionForSpline();
            }
            else
            {
                UpdatePosition();
            }
        }

        public void PauseMovement()
        {
            Halted = true;
        }

        public void ResumeMovement()
        {
            Halted = false;
        }
        
        protected virtual void UpdatePosition()
        {
            if(CurrentWaypoint == null && !_useSpline) return;
      
            Vector3 error = _targetPosition - GetCurrentPosition();
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
            Vector3 moveDirection = direction;
            // if (LockForwardMovement)
            // {
            //     moveDirection = transform.forward;
            // }
            Vector3 positionUpdate =  moveDirection * deltaTime * Speed;

            SetRotation(Quaternion.LookRotation(direction));
            SetPosition(GetCurrentPosition() + positionUpdate);
        }

        private float _currentSplineTSpeed = 0f;
        private void UpdatePositionForSpline()
        {
            SetPosition(GetSplineTargetPosition());
            Vector3 direction = _rotationTarget - GetCurrentPosition();
            float turnAngle = Vector3.Angle(transform.forward, direction);
            float turnSpeedFactor = Mathf.Lerp(1f, MinTurnSpeedFactor, Mathf.Clamp(turnAngle / MaxTurnAngle, MinTurnSpeedFactor, 1));  // 0° = full speed, 90° or more = 50% speed
            UpdateRotation(_rotationTarget - transform.position);

            float targetSpeed = _splineTSpeed;
            targetSpeed = Mathf.Min(targetSpeed, _splineTSpeed);
            // if (_currentSplineTSpeed > targetSpeed)
            // {
            //  _currentSplineTSpeed -= Acceleration * Time.deltaTime;
            // }
            // else if(_currentSplineTSpeed < targetSpeed)
            // {
            //  _currentSplineTSpeed += Acceleration * Time.deltaTime;
            // }
            _currentSplineTSpeed = targetSpeed;
            _splineT += Time.deltaTime * (_currentSplineTSpeed / GetArcTotalLength());
            if (_splineT >= 1.0f)
            {
                var lastWp = WaypointsList[^1];
                Moving = false;
                SnapToPosition(lastWp.Position, lastWp.Rotation);
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

            SetRotation(Quaternion.Slerp(GetCurrentRotation(), Quaternion.LookRotation(targetDirection),
                Time.deltaTime * RotationLerpFactor));
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

            // Use the normalized arc lengths to map _splineT to the correct segment
            for (int i = 1; i < _normalizedArcLengths.Length; i++)
            {
                if (_splineT <= _normalizedArcLengths[i])
                {
                    int minIndex = i - 1;
                    int maxIndex = i;
                    int rotationTargetIndex = maxIndex + SplineRotationLookAhead;
                    rotationTargetIndex = Mathf.Min(rotationTargetIndex, lengthOfArray - 1);
                    _rotationTarget = _smoothSplinePoints[rotationTargetIndex];
                    // Find the local t value between the two points
                    float segmentT = (_splineT - _normalizedArcLengths[minIndex]) / (_normalizedArcLengths[maxIndex] - _normalizedArcLengths[minIndex]);

                    // Interpolate the position
                    Vector3 floorPos = _smoothSplinePoints[minIndex];
                    Vector3 ceilPos = _smoothSplinePoints[maxIndex];
                    return Vector3.Lerp(floorPos, ceilPos, segmentT);
                }
            }
            _rotationTarget = _smoothSplinePoints[^1];
            return _smoothSplinePoints[lengthOfArray - 1]; 
        }
        private void SetNextWaypoint()
        {
            _targetPosition = GetTargetPosition();
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
            if (AnchorPoint != null)
            {
                position = position - (transform.forward * AnchorPoint.transform.localPosition.z) ;
            }
            transform.position = position;
        }

        public void SetRotation(Quaternion rotation)
        {
            transform.rotation = rotation;
        }
        
        private void SnapToPosition(Vector3 position, Quaternion rotation)
        {
            _moveTween = DOTween.To(GetCurrentPosition, SetPosition, position, 0.05f).OnComplete(()=>{
                OnReachedFinalTarget?.Invoke();
            });
            _rotateTween = transform.DORotate(rotation.eulerAngles, 0.05f).SetEase(Ease.InOutQuad);

        }
        public bool IsMoving()
        {
            return CurrentWaypoint != null;
        }
    }
}