using System;
using UnityEngine;
using Kuantech.Utils;

namespace Kuantech.Core
{
    [Serializable]
    public struct CameraParameters
    {
        public SphericalCoordinate Spherical;
        public Vector3 PositionOffset;
        public Vector3 LookatOffset;
    }
    
    public class CameraFollower : MonoBehaviour
    {
        public CameraParameters CameraParameters;
        [SerializeField] public Transform Target;
        [SerializeField] public float FollowDistance = 5.0f;
        [SerializeField] public float horizontalSensitivity = 1.0f;
        [SerializeField] public float verticalSensitivity = 1.0f;
        [SerializeField] public float Speed = 10.0f;

        [Header("Lerp Factors")] 
        [SerializeField] private float PositionLerpFactor = 1f;
        [SerializeField] private float RotationSlerpFactor = 1f;
        [SerializeField] private float ZoomLerpFactor = 10f;

        private Vector3 _targetPosition;
        private Vector3 _targetLookAt;

        private float _yawAccel = 0f;
        private float _pitchAccel = 0f;
        private float _yawSpeed = 0f;
        private float _pitchSpeed = 0f;

        private float _deltaX = 0f;
        private float _deltaY = 0f;

        private float _zoomFactorTarget = 1f;
        private float _currentZoomFactor = 1f;
        
        private void Start()
        {
        }


        public void SetDeltaX(float deltaX)
        {
            _deltaX = deltaX;
        }

        public void SetDeltaY(float deltaY)
        {
            _deltaY = deltaY;
        }
        private void Update()
        {
            if (Target == null) return;

            float zoomedFollow = FollowDistance * _currentZoomFactor;

            _currentZoomFactor = Mathf.Lerp(_currentZoomFactor, _zoomFactorTarget, Time.deltaTime * ZoomLerpFactor);
            
            _yawAccel = horizontalSensitivity * _deltaX;
            _pitchAccel = verticalSensitivity * _deltaY;
            
            float yawSpeed = _yawSpeed + Time.deltaTime * _yawAccel;
            float pitchSpeed = _pitchSpeed + Time.deltaTime * _pitchAccel;

            CameraParameters.Spherical.Yaw -= _yawSpeed * Time.deltaTime + _yawAccel * Time.deltaTime * Time.deltaTime * 0.5f;  
            CameraParameters.Spherical.Pitch += _pitchSpeed * Time.deltaTime + _pitchAccel * Time.deltaTime * Time.deltaTime * 0.5f;

            _yawSpeed = yawSpeed;
            _pitchSpeed = pitchSpeed;

            CameraParameters.Spherical.Pitch = Mathf.Clamp(CameraParameters.Spherical.Pitch, Mathf.Deg2Rad * 5.0f, Mathf.Deg2Rad*175.0f);
            CameraParameters.Spherical.Radius = zoomedFollow;

            _yawSpeed *= 0.9f;
            _pitchSpeed *= 0.9f;
            
            //Get cam forward
            
            // camForward = Target.transform.forward;
            // camRight = Target.transform.right;
            Vector3 targetPos = GetTargetPosition();
            Vector3 lookTarget = GetLookAtTarget(targetPos);
            float angleDiff = Mathf.Asin(CameraParameters.LookatOffset.x / CameraParameters.Spherical.Radius);
            Vector3 sphericalPos = Quaternion.AngleAxis(Target.transform.localRotation.eulerAngles.y, Vector3.up) * SphericalCoordinate.ToWorld(CameraParameters.Spherical.Radius, 
                CameraParameters.Spherical.Yaw + angleDiff, 
                CameraParameters.Spherical.Pitch) + targetPos;
            
            transform.position = Vector3.Lerp(transform.position, sphericalPos, PositionLerpFactor);
            // targetTransform.LookAt(lookTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookTarget - transform.position, Vector3.up), RotationSlerpFactor);
        }

        protected virtual Vector3 GetTargetPosition()
        {
            return Target.position + (Quaternion.AngleAxis(Target.transform.localRotation.eulerAngles.y, Vector3.up) * CameraParameters.PositionOffset);
        }

        protected virtual Vector3 GetLookAtTarget(Vector3 targetPos)
        {
            Vector3 camForward = CameraParameters.Spherical.GetForward().normalized;
            Vector3 camRight = -1 * Vector3.Cross(camForward.normalized, Vector3.up).normalized;
            return camRight * CameraParameters.LookatOffset.x + camForward * CameraParameters.LookatOffset.z 
                                                              + Vector3.up * CameraParameters.LookatOffset.y + targetPos;
        }
        
        public virtual void SetTargetParameters(CameraParameters cameraParameters)
        {
            CameraParameters = new CameraParameters
            {
                LookatOffset = cameraParameters.LookatOffset,
                PositionOffset = cameraParameters.LookatOffset,
                Spherical = cameraParameters.Spherical,
            };
        }
        
        /// <summary>
        /// Zooms in or out by adjusting zoom in factor. If zoomInFactor>1 zoom in, otherwise zoomout 
        /// </summary>
        /// <param name="zoomInFactor"></param>
        public void Zoom(float zoomInFactor)
        {
            _zoomFactorTarget = zoomInFactor;
        }

        public void ResetZoom()
        {
            _zoomFactorTarget = 1f;
        }
    }
}
