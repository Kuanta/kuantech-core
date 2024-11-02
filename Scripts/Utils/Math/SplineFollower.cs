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

        public BSpline CurrentSpline = null;
        public float FollowSpeed;
        public float FollowSpeedT;
        public int Direction = 1;
        public bool IsMoving = false;
        public bool Paused = false;
        
        [Header("Thresholds")]
        public float DistanceThreshold = 0.1f;
        public float TThreshold = 0.01f;

        public FollowMethod CurrentFollowMethod = FollowMethod.FollowWithDistance;
        
        private float _currentT;
        private float _currentDistance;

        private float _targetDistance;
        private float _targetT;

        public UnityAction OnReachedTarget;
        
        #region Controls
        public void GoToDistance(float distance)
        {
            CurrentFollowMethod = FollowMethod.FollowWithDistance;
            _targetDistance = distance;
            IsMoving = true;
            Paused = false;
        }
        #endregion
        private void Update()
        {
            if (!IsMoving || Paused || CurrentSpline == null) return;
            if (CurrentFollowMethod == FollowMethod.FollowWithDistance)
            {
                UpdateWithDistance();
            }
        }
        
        private void UpdateWithDistance()
        {
            float error = _targetDistance - _currentDistance;
            if (Mathf.Abs(error) < DistanceThreshold)
            {
                ReachedTarget();
                return;
            }

            float movementUpdate = Mathf.Min(Time.deltaTime * FollowSpeed, Mathf.Abs(error)) * Mathf.Sign(error);
            _currentDistance += movementUpdate;
            SetPositionWithDistance(_currentDistance);
        }

        #region Follow

        private void ReachedTarget()
        {
            OnReachedTarget?.Invoke();
            
            //Snap to target?
            //SetPositionWithDistance(_targetDistance);
            IsMoving = false;
        }

        public void Stop()
        {
            IsMoving = false;
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
            transform.rotation = point.Rotation;
        }

        #endregion
       
    }
}