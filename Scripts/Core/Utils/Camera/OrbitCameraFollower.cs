using System;
using Kuantech.Core.Camera;
using Kuantech.Core.Controller;
using UnityEngine;

namespace Kuantech.Core.Utils
{
    public class OrbitCameraFollower : MonoBehaviour
    {
        public KtCamera Camera;
        public Transform Anchor;
        public Vector3 AnchorOffset = Vector3.zero;
        public bool InvertY = false;

        public float YawOffset = 0f;
        public bool UseTargetValues = false;
        public float Radius => _currentRadius;
        public float PitchAngle => _currentPitchAngle;
        public float YawAngle => _currentYawAngle;
        
        [Header("Angle Smoothing (seconds)")]
        public float RotationSlerpFactor = 100f;
        public float YawSmoothTime   = 0.08f;
        public float PitchSmoothTime = 0.08f;
        public float RadiusSmoothTime= 0.12f;
        
        [Header("Position Smoothing")]
        public float PositionSmoothTime = 0.10f;
        public float MaxFollowSpeed     = Mathf.Infinity;
        public float LookAheadTime      = 0.0f;  
        
        [Header("Limits")]
        public float MinPitch = -89f;
        public float MaxPitch =  89f;
        public float MinRadius = 0.1f;
        public float MaxRadius = 50f;
        
        private float _currentPitchAngle;
        private float _currentYawAngle;
        private float _currentRadius;

        [SerializeField] private float _targetPitchAngle;
        [SerializeField] private float _targetYawAngle;
        [SerializeField] private float _targetRadius = 5;
        
        [NonSerialized] public PlayerController Controller;
        public bool UseControllerYawPitch = false;

        private float _yawVel;
        private float _pitchVel;
        private float _radiusVel;
        private Vector3 _positionVel;

        private void LateUpdate()
        {
            // 1) Update targets
            if (UseControllerYawPitch)
            {
                Controller = ControllerManager.GetCurrentController();
                if (Controller != null)
                {
                    // Controller kendi tarafında zaten sönümlüyse bile sorun değil; burada ikinci bir tatlı damping olacak
                    float targetYaw   = Normalize360(UseTargetValues ? Controller.TargetYaw : Controller.Yaw);
                    float targetPitch = Mathf.Clamp(UseTargetValues ? Controller.TargetPitch : Controller.Pitch * (InvertY ? -1f : 1f), MinPitch, MaxPitch);
                    SetYaw(targetYaw);
                    SetPitch(targetPitch);
                }
            }
            float dt = Mathf.Max(Time.deltaTime, 1e-6f);

            
            // 2) Update angle values

            _currentYawAngle = Mathf.SmoothDampAngle(
                _currentYawAngle, _targetYawAngle, ref _yawVel, YawSmoothTime, Mathf.Infinity, dt);

            _currentPitchAngle = Mathf.SmoothDampAngle(
                _currentPitchAngle, _targetPitchAngle, ref _pitchVel, PitchSmoothTime, Mathf.Infinity, dt);

            _currentPitchAngle = Mathf.Clamp(_currentPitchAngle, MinPitch, MaxPitch);

            _currentRadius = Mathf.SmoothDamp(
                _currentRadius, _targetRadius, ref _radiusVel, RadiusSmoothTime, Mathf.Infinity, dt);
            _currentRadius = Mathf.Clamp(_currentRadius, MinRadius, MaxRadius);
            
            Transform traansformToUpdate = GetTransformToUpdatePosition();
            Vector3 targetPosition = GetTargetPosition();

            traansformToUpdate.rotation = GetTargetRotation();
            
            // Position
            Vector3 pos = Vector3.SmoothDamp(
                transform.position, targetPosition, ref _positionVel, PositionSmoothTime, MaxFollowSpeed, Time.deltaTime);
            traansformToUpdate.position = pos;
        }

        private Transform GetTransformToUpdatePosition()
        {
            if (Camera == null) return transform;
            return transform;
        }
        
        public void SetPitch(float pitch, bool immediate = false)
        {
            _targetPitchAngle = pitch;
            if(immediate) _currentPitchAngle = pitch;
        }
        
        public void SetYaw(float yaw, bool immediate = false)
        {
            _targetYawAngle = yaw;
            if(immediate) _currentYawAngle = yaw;
        }
        
        public void SetRadius(float radius, bool immediate = false)
        {
            _targetRadius = radius;
            if (immediate) _currentRadius = radius;
        }
        
        public Vector3 GetTargetPosition()
        {
            Transform anchor = GetAnchor();
            if (!anchor) return transform.position;

            // Spherical coordinates → cartesian (Yaw: around Y, Pitch: around X)
            float yawRad   = Mathf.Deg2Rad * _currentYawAngle + YawOffset * Mathf.Deg2Rad;
            float pitchRad = Mathf.Deg2Rad * _currentPitchAngle;

            float cp = Mathf.Cos(pitchRad);
            Vector3 sphericalOffset = new Vector3(
                _currentRadius * cp * Mathf.Sin(yawRad),
                _currentRadius * Mathf.Sin(pitchRad),
                _currentRadius * cp * Mathf.Cos(yawRad)
            );
            
            return anchor.position + sphericalOffset + GetAnchorOffset();
        }

        public Vector3 GetAnchorOffset()
        {
            float yawRad   = Mathf.Deg2Rad * _currentYawAngle + YawOffset * Mathf.Deg2Rad;
            return (new Vector3(-1*Mathf.Cos(yawRad), 0, Mathf.Sin(yawRad))) * AnchorOffset.x + Vector3.up * AnchorOffset.y +
                                  (new Vector3(Mathf.Sin(yawRad), 0, -1*Mathf.Cos(yawRad))) * AnchorOffset.z;
        }
        
        public Quaternion GetTargetRotation()
        {
            Transform anchor = GetAnchor();
            if (!anchor) return transform.rotation;

            Vector3 targetPos = GetTargetPosition();
            Vector3 toAnchor  = (anchor.position + GetAnchorOffset()) - targetPos;
            if (toAnchor.sqrMagnitude < 1e-8f) return transform.rotation;

            return Quaternion.LookRotation(toAnchor.normalized, Vector3.up);
        }
    
        public Transform GetAnchor() => Anchor ? Anchor : transform;

        private static float Normalize360(float a) => (a % 360f + 360f) % 360f;
    }
}