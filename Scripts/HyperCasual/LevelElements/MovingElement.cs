using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public class MovingElement : LevelElement
    {
        [Header("Rigidbody")]
        
        [Header("Translation")] 
        public int InitialWaypointIndex = 0;
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
            
            if (IsMoving)
            {
                Vector3 diff = Waypoints[_currentWaypointIndex].position - MovingPart.transform.position;
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
                    Translate(diff);
                }

            }

            if (!IsRotation) return;
            if (Angles.Count <= 1)
            {
                transform.Rotate(RotationAxis, AngularSpeed * Time.deltaTime);
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
                float angleChange = Time.deltaTime * Math.Sign(angleDiff) * AngularSpeed;
                _currentAngle += angleChange;
                RotatingPart.localRotation = Quaternion.AngleAxis(_currentAngle, RotationAxis);
                _previousAngleChange = angleChange;
            }
        }

        protected virtual void Translate(Vector3 diff)
        {
            Vector3 displacement = diff.normalized * (Time.deltaTime * MovementSpeed);
            MovingPart.transform.localPosition += displacement;
            _previousDisplacement = displacement;
        }
        public override void OnPrepareLevel()
        {
            Reset();
        }

        public override void OnLeaveLevel()
        {
        }

        public override void OnPlayLevel()
        {
            if (IsRotation)
            {
                InitialAngle = Angles[InitialAngleIndex];
                _currentAngle = Angles[InitialAngleIndex];
                _currentTargetAngleIndex = InitialAngleIndex;
                RotatingPart.localRotation = Quaternion.AngleAxis(_currentAngle, RotationAxis);
            }
            else
            {
                _currentWaypointIndex = InitialWaypointIndex;
            }
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