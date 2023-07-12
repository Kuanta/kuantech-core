using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public class MovingElement : LevelElement
    {
        [Header("Rigidbody")] 
        [SerializeField] private Rigidbody Rigidbody;
        [SerializeField] private bool UseRigidbody;
        
        [Header("Translation")] 
        public int InitialWaypointIndex = 0;
        public float InitialPositionFactor = 0f; //
        [SerializeField] private bool IsMoving = false;
        [SerializeField] private Transform MovingPart;
        [SerializeField] private float MovementSpeed = 1;
        [SerializeField] private List<Transform> Waypoints;
        [SerializeField] private int _currentWaypointIndex;
        private Vector3 _previousDisplacement = Vector3.zero;
        
        [Header("Rotation")] 
        public int InitialAngleIndex;
        [SerializeField] private bool IsRotation = false;
        [SerializeField] private Transform RotatingPart;
        [SerializeField] private float AngularSpeed;
        [SerializeField] private List<float> Angles;
        [SerializeField] private Vector3 RotationAxis;
        [SerializeField] private int _currentTargetAngleIndex;
        [SerializeField] private float InitialAngle = 0f;
        private float _currentAngle = 0f;
        private float _previousAngleChange = 0f;
        
        [Header("Delay")]
        public float WaitDelay = 0f;
        private bool _delaying = false;
        private float _delayStartTime;

        private void Update()
        {
            if (_delaying)
            {
                if (Time.time - _delayStartTime > WaitDelay) _delaying = false;
                else return;
            }
            if(!UseRigidbody) HandleUpdate();
        
        }

        private void FixedUpdate()
        {
            HandleUpdate();
        }
        private void HandleUpdate()
        {
            if (_delaying) return;
            float deltaTime = UseRigidbody ? Time.fixedDeltaTime : Time.deltaTime;
            if (IsMoving)
            {
                Vector3 diff = Waypoints[_currentWaypointIndex].localPosition - MovingPart.transform.localPosition;
                if (Vector3.Dot(diff, _previousDisplacement) < 0 || diff.sqrMagnitude <= 0.01f)
                {
                    //Reached
                    _currentWaypointIndex++;
                    _currentWaypointIndex %= Waypoints.Count;
                    _previousDisplacement = Vector3.zero;
                    if (WaitDelay > 0) _delaying = true;
                }
                else
                {
                    Translate(diff, deltaTime);
                }

            }

            if (!IsRotation) return;
            if (Angles.Count <= 1)
            {
                Quaternion rot = transform.localRotation * Quaternion.AngleAxis(AngularSpeed * deltaTime, RotationAxis);
                Rotate(rot);
                return;
            }
            float angleDiff = Angles[_currentTargetAngleIndex] - _currentAngle;
            int direction = Math.Sign(angleDiff);
            if(angleDiff == 0 || (_previousAngleChange != 0 && direction != Math.Sign(_previousAngleChange)))
            {
                //Reached
                _currentTargetAngleIndex++;
                _currentTargetAngleIndex %= Angles.Count;
                _previousAngleChange = 0;
                if (WaitDelay > 0) _delaying = true;
            }
            else
            {
                float angleChange = deltaTime * Math.Sign(angleDiff) * AngularSpeed;
                _currentAngle += angleChange;
                RotatingPart.localRotation = Quaternion.AngleAxis(_currentAngle, RotationAxis);
                _previousAngleChange = angleChange;
            }
        }

        private void Rotate(Quaternion rotation)
        {
            if (UseRigidbody)
            {
                Rigidbody.MoveRotation(rotation);
            }
            else
            {
                RotatingPart.localRotation = rotation;
            }
        }
        protected virtual void Translate(Vector3 diff, float deltaTime)
        {
            Vector3 displacement = diff.normalized * (deltaTime * MovementSpeed);
            MovingPart.transform.localPosition += displacement;
            _previousDisplacement = displacement;
        }
        public override void OnPrepareLevel()
        {
            Reset();
            if (IsRotation)
            {
                if (Angles.Count > 0)
                {
                    InitialAngle = Angles[InitialAngleIndex];
                    _currentTargetAngleIndex = InitialAngleIndex;
                }
                _currentAngle = InitialAngle;
                Debug.LogError("Initial Angle:"+InitialAngle);
                RotatingPart.localRotation = Quaternion.AngleAxis(_currentAngle, RotationAxis);
            }
            else
            {
                _currentWaypointIndex = InitialWaypointIndex;
                if (Waypoints.Count < 2) return;
                int nextWaypointIndex = (_currentWaypointIndex + 1) % Waypoints.Count;
                Vector3 diff = Waypoints[nextWaypointIndex].localPosition - Waypoints[_currentWaypointIndex].localPosition;
                float distance = diff.magnitude * InitialPositionFactor;
                MovingPart.transform.localPosition =
                    Waypoints[_currentWaypointIndex].localPosition + diff.normalized * distance;
            }
        }

        public override void OnRestartLevel()
        {
            Reset();
        }

        public override void OnLeaveLevel()
        {
        }

        public override void OnPlayLevel()
        {
        }

        public override void OnPlayerEntered()
        {
        }

        public override void OnPlayerExited()
        {
        }

        public void Reset()
        {
            _currentWaypointIndex = 0;
            _currentTargetAngleIndex = 0;
            _delaying = false;

            _previousDisplacement = Vector3.zero;
            _previousAngleChange = 0f;
            if(MovingPart != null) MovingPart.transform.localPosition = Vector3.zero;
            if(RotatingPart != null) RotatingPart.localRotation = Quaternion.identity;
            _currentAngle = 0f;
        }
    }
}