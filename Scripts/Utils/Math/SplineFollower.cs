using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Utils.Math
{
    public class SplineFollower : MonoBehaviour
    {
        public enum FollowMethod
        {
            FollowWithDistance,
            FollowWithT,
        }

        public Vector3 FollowRotationVector = new Vector3(0, 0, 1);
        public BSpline CurrentSpline = null;
        public int SplineDegree = 3;
        public int SplineResolution = 10;
        public int RotationLookAhead;
        public float FollowSpeed;
        public int Direction = 1;
        public bool Moving = false;
        public bool Paused = false;
        public bool SetRotation = true;
        
        [Header("Thresholds")]
        public float DistanceThreshold = 0.1f;
        public float TThreshold = 0.01f;
        public FollowMethod CurrentFollowMethod = FollowMethod.FollowWithDistance;
        
        private float _currentT;
        private float _currentDistance = -1;

        private float _targetDistance;
        private float _targetT;

        public UnityAction OnReachedTarget;

        [NonSerialized] public bool StopAtReachindTarget = false;

        #region Spline Creation

        public void SetSpline(List<Vector3> points)
        {
            CurrentSpline = new BSpline();
            CurrentSpline.SetSplinePoints(points, SplineDegree, SplineResolution);
            CurrentSpline.RotationLookAhead = RotationLookAhead;
        }

        #endregion
        #region Controls
        public void GoToDistance(float distance)
        {
            CurrentFollowMethod = FollowMethod.FollowWithDistance;
            StopAtReachindTarget = true;
            _targetDistance = distance;
            Moving = true;
            Paused = false;
        }

        public void GoToPercentage(float t)
        {
            t = Mathf.Clamp01(t);
            float distanceAtT = CurrentSpline.GetTotalDistance() * t;
            GoToDistance(distanceAtT);
        }
        #endregion
        private void Update()
        {
            if (!Moving || Paused || CurrentSpline == null) return;
            if (CurrentFollowMethod == FollowMethod.FollowWithDistance)
            {
                UpdateWithDistance();
            }
        }
        private void UpdateWithDistance()
        {
            float movementUpdate = Time.deltaTime * FollowSpeed;
            //If targeted
            if (StopAtReachindTarget)
            {
                float error = _targetDistance - _currentDistance;
                movementUpdate = Mathf.Min(movementUpdate, Mathf.Abs(error)) * Mathf.Sign(error);

                if (Mathf.Abs(error) < DistanceThreshold)
                {
                    ReachedTarget();
                    return;
                }
            }
            _currentDistance += movementUpdate;
            _currentDistance = CurrentSpline.ClampDistance(_currentDistance);
            SetPositionWithDistance(_currentDistance);
        }
        #region Follow

        public void StartMove()
        {
            StopAtReachindTarget = false;
            Moving = true;
        }
        
        public void SetSpeed(float speed)
        {
            CurrentFollowMethod = FollowMethod.FollowWithDistance;
            FollowSpeed = speed;
        }
        
        /// <summary>
        /// Sets a spline and follows it to the end
        /// </summary>
        /// <param name="spline"></param>
        public void FollowSpline(BSpline spline=null)
        {
            if (spline == null && CurrentSpline == null) return;
            if(spline != null) CurrentSpline = spline;
            SetPositionWithDistance(0);
            GoToPercentage(1f);
        }
        
        public void SetCurrentDistance(float distance)
        {
            _currentDistance = distance;
            SetPositionWithDistance(_currentDistance);
        }

        public float GetCurrentDistance()
        {
            return _currentDistance;
        }
        
        /// <summary>
        /// Sets the current distance with t
        /// </summary>
        /// <param name="t"></param>
        public void SetCurrentDistanceWithT(float t)
        {
            _currentDistance = CurrentSpline.GetDistanceAtT(t);
            SetPositionWithDistance(_currentDistance);
        }
        
        private void ReachedTarget()
        {
            //Snap to target?
            SetPositionWithDistance(_targetDistance);
            Moving = false;
            OnReachedTarget?.Invoke();
        }

        public void Stop()
        {
            Moving = false;
        }

        public bool IsMoving()
        {
            return Moving && !Paused;
        }
    
        public void Halt()
        {
            Paused = true;
        }

        public void Resume()
        {
            Paused = false;
        }
        #endregion
        
        #region Position Setters
        public void SetPositionWithT(float t)
        {
            _currentT = t;
            WorldPoint pointAtT = CurrentSpline.GetPointAtT(t);
            SetPosition(pointAtT);
        }

        public void SetPositionWithDistance(float distance)
        {
            _currentDistance = distance;
            SetPosition(CurrentSpline.GetPointAtDistance(distance));
        }
        
        private void SetPosition(WorldPoint point)
        {
            transform.position = point.Position;
            if(SetRotation) transform.rotation = Quaternion.FromToRotation(FollowRotationVector, point.Rotation * Vector3.forward);
        }
        #endregion
       
    }
}