using System;
using DG.Tweening;
using UnityEngine;
using Kuantech.Utils;
using Unity.VisualScripting;
using UnityEngine.Events;

namespace Kuantech.Core
{
    [Serializable]
    public struct CameraParameters
    {
        public SphericalCoordinate Spherical;
        public Vector3 PositionOffset;
        public Vector3 LookatOffset;
        
        //If LookAt is enabled, rotation will be calculated from lookAt
        public bool LookAt;
        public Vector3 LookAtAngles;
    }

    public struct TransitionParameters
    {
        public bool TransitionPosition; //If set to true, position will be transitioned
        public Transform FocusObjcet; //If not null, transition will be made by focusing this point
        public Vector3 TargetPosition; //If targetObject is null, this will be used
        public float PositionTransitionTime;
        public bool TransitionRotation; //If set to true, rotation transition
        public Quaternion TargetRotation;
        public bool LookToFocus; //If set to true, camera will look towards focus
        public float RotationTransitionTime;
    }
    
    public class CameraFollower : MonoBehaviour
    {
        public CameraParameters CameraParameters;
        public bool Following = true;
        [SerializeField] public Transform Target;
        [SerializeField] public float FollowDistance = 5.0f;
        [SerializeField] public float horizontalSensitivity = 1.0f;
        [SerializeField] public float verticalSensitivity = 1.0f;
        [SerializeField] public float Speed = 10.0f;

        [Header("Lerp Factors")] 
        [SerializeField] private float PositionLerpFactor = 1f;
        [SerializeField] private float RotationSlerpFactor = 1f;
        [SerializeField] private float ZoomLerpFactor = 10f;
        
        private Vector3 _desiredPosition;
        private Quaternion _desiredRotation;

        private float _yawAccel = 0f;
        private float _pitchAccel = 0f;
        private float _yawSpeed = 0f;
        private float _pitchSpeed = 0f;

        private float _deltaX = 0f;
        private float _deltaY = 0f;

        private float _zoomFactorTarget = 1f;
        private float _currentZoomFactor = 1f;
        
        //Transitioning
        private bool Transitioning => _positionTransition || _rotationTransition;
        private bool _positionTransition;
        private bool _rotationTransition;
        private UnityAction _transitionComplete;
        
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
            if (Transitioning)
            {
                return;
            }
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

            CameraParameters.Spherical.Pitch = Mathf.Clamp(CameraParameters.Spherical.Pitch, -Mathf.Deg2Rad*179.9f, Mathf.Deg2Rad*179.9f);
            CameraParameters.Spherical.Radius = zoomedFollow;

            _yawSpeed *= 0.9f;
            _pitchSpeed *= 0.9f;
            
            Vector3 desiredPos = GetDesiredPosition();
            Quaternion desiredRotation = GetDesiredRotation(Target, desiredPos);
            
            transform.position = Vector3.Lerp(transform.position, desiredPos, PositionLerpFactor);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, RotationSlerpFactor);
        }
        
        /// <summary>
        /// Returns the desired position for the camera
        /// </summary>
        /// <returns></returns>
        protected virtual Vector3 GetDesiredPosition()
        {
            if (Target == null) return _desiredPosition;
            
            _desiredPosition = GetDesiredPositionForObject(Target);
            return _desiredPosition;
        }
        
        /// <summary>
        /// Returns the desired rotation for the camera
        /// </summary>
        /// <returns></returns>
        protected virtual Quaternion GetDesiredRotation(Transform target, Vector3 position)
        {
            if (!CameraParameters.LookAt)
            {
                return Quaternion.Euler(CameraParameters.LookAtAngles);
            }
            if (target == null) return _desiredRotation;
            return GetDesiredRotationForObject(target, position);
        }

        private Quaternion GetDesiredRotationForObject(Transform target, Vector3 fromPosition)
        {
            Vector3 lookTarget = GetLookAtPosition(GetFocusPoint(target));
            return Quaternion.LookRotation(lookTarget - fromPosition, Vector3.up);
        }
        /// <summary>
        /// Returns the look position for a given position
        /// </summary>
        /// <param name="targetPosition"></param>
        /// <returns></returns>
        protected virtual Vector3 GetLookAtPosition(Vector3 targetPosition)
        {
            Vector3 camForward = CameraParameters.Spherical.GetForward().normalized;
            Vector3 camRight = -1 * Vector3.Cross(camForward.normalized, Vector3.up).normalized;
            return camRight * CameraParameters.LookatOffset.x + camForward * CameraParameters.LookatOffset.z 
                                                              + Vector3.up * CameraParameters.LookatOffset.y + targetPosition;
        }

        /// <summary>
        /// Returns the camera's should be position for a given target
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private Vector3 GetDesiredPositionForObject(Transform target)
        {
            Vector3 focusPoint = GetFocusPoint(target);
            float angleDiff = Mathf.Asin(CameraParameters.LookatOffset.x / CameraParameters.Spherical.Radius);
            Vector3 sphericalPos = Quaternion.AngleAxis(target.transform.rotation.eulerAngles.y, Vector3.up) * SphericalCoordinate.ToWorld(CameraParameters.Spherical.Radius, 
                CameraParameters.Spherical.Yaw + angleDiff, 
                CameraParameters.Spherical.Pitch) + focusPoint;
            return sphericalPos;
        }
        
        /// <summary>
        /// Returns the focus point for an object. Simply adds the position offset according to the focus object's rotation
        /// </summary>
        /// <param name="focusedObject"></param>
        /// <returns></returns>
        private Vector3 GetFocusPoint(Transform focusedObject)
        {
            return focusedObject.position + (Quaternion.AngleAxis(focusedObject.rotation.eulerAngles.y, Vector3.up) * CameraParameters.PositionOffset);
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
        
        #region PublicInterface
        public virtual void SetCameraParameters(CameraParameters cameraParameters)
        {
            CameraParameters = cameraParameters;
            
            //Set spherical coordinates with new class since class members are passed by ref
            CameraParameters.Spherical = new SphericalCoordinate(cameraParameters.Spherical.Radius, cameraParameters.Spherical.Yaw, cameraParameters.Spherical.Pitch);
        }
        
        /// <summary>
        /// Sets the target position for camera
        /// </summary>
        /// <param name="desiredPosition"></param>
        public void SetDesiredPosition(Vector3 desiredPosition)
        {
            _desiredPosition = desiredPosition;
        }

        public void SetDesiredPositionForTarget(Transform target)
        {
            _desiredPosition = GetDesiredPositionForObject(target);
        }
        
        /// <summary>
        /// Sets the target rotation for camera
        /// </summary>
        /// <param name="desiredRotation"></param>
        public void SetDesiredRotation(Quaternion desiredRotation)
        {
            _desiredRotation = desiredRotation;
        }

        public void SetTarget(Transform target)
        {
            Target = target;
        }
        #endregion
        
        #region Transition

        
        /// <summary>
        /// Warps the camera to desired position 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="setTarget"></param>
        public void WarpCamera(Transform target, bool setTarget = true)
        {
            _desiredPosition = GetDesiredPositionForObject(target);
            _desiredRotation = GetDesiredRotation(target, _desiredPosition);

            transform.position = _desiredPosition;
            transform.rotation = _desiredRotation;
            if(setTarget) Target = target;
        }

        public void Transition(TransitionParameters transitionParameters, UnityAction transitionCompleteHandler)
        {
            _positionTransition = transitionParameters.TransitionPosition;
            _rotationTransition = transitionParameters.TransitionRotation;
            if (! _positionTransition && !_rotationTransition)
            {
                Debug.LogWarning("No rotation or position transition is set to true");
                _transitionComplete = null;
                return;
            }
            _transitionComplete = transitionCompleteHandler;

            Vector3 targetPosition = transform.position;
            if (_positionTransition)
            {
                targetPosition = transitionParameters.FocusObjcet != null ? GetDesiredPositionForObject(transitionParameters.FocusObjcet) : transitionParameters.TargetPosition;
                transform.DOMove(targetPosition, transitionParameters.PositionTransitionTime).OnComplete(() =>
                {
                    _positionTransition = false;
                    if(!_rotationTransition) transitionCompleteHandler?.Invoke();
                });
            }

            if (_rotationTransition)
            {
                Quaternion targetRotation = transitionParameters.FocusObjcet != null && transitionParameters.LookToFocus
                    ? GetDesiredRotationForObject(transitionParameters.FocusObjcet, targetPosition)
                    : transitionParameters.TargetRotation;
                
                
                transform.DORotateQuaternion(targetRotation, transitionParameters.RotationTransitionTime).OnComplete(() =>
                {
                    _rotationTransition = false;
                    if(!_positionTransition) transitionCompleteHandler?.Invoke();
                });
            }

        }

        #endregion
    }
}
