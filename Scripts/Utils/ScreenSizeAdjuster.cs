using DG.Tweening;
using Kuantech.Core.Camera;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Core.Utils
{
    public class ScreenSizeAdjuster : MonoBehaviour {
        [Header("Anchors")]
        public GameObject TopAnchor;
        public GameObject BottomAnchor;
        public GameObject LeftAnchor;
        public GameObject RightAnchor;
        public Vector3 CameraOffset;

        [Header("Angles")] 
        public float PitchAngle;
        public float YawAngle;
        
        [Header("Perspective")]
        public bool FitOnUpdate = true;

        [Header("Animation")] public AnimationCurve EaseAnimationCurve;
        public float AnimationDuration = 2;

        public KtCamera CameraRig;

        //public Camera CameraToFit;

        private Vector3 _targetPosition;
        private Vector3 _targetNormal;
        protected virtual void Update()
        {
            if(!FitOnUpdate) return;
            CalculateTargetParameters();
            Fit();
        }

        public void SetNewAnchors(GameObject top, GameObject right, GameObject bottom, GameObject left)
        {
            TopAnchor = top;
            RightAnchor = right;
            BottomAnchor = bottom;
            LeftAnchor = left;
            CalculateTargetParameters();
        }

        public void GoToTargetPosition(float duration, UnityAction onReached)
        {
            if (CameraRig == null) return;
            CameraRig.transform.DOMove(_targetPosition, AnimationDuration).OnComplete(() =>
            {
                onReached?.Invoke();
            }).SetEase(EaseAnimationCurve);
            Quaternion targetRot = Quaternion.LookRotation(_targetNormal, Vector3.up);
            CameraRig.transform.DORotate(targetRot.eulerAngles, duration);
        }
        
        public void Fit()
        {
            if (CameraRig == null) return;
            CameraRig.transform.forward = _targetNormal;
            CameraRig.transform.position = _targetPosition;
        }

        private void CalculateTargetParameters()
        {
            if (CameraRig == null) return;
            // Get the positions of the anchor points
            Vector3 topPosition = TopAnchor.transform.position;
            Vector3 leftPosition = LeftAnchor.transform.position;
            Vector3 rightPosition = RightAnchor.transform.position;
            Vector3 bottomPosition = BottomAnchor.transform.position;

            Vector3 forward = (topPosition - bottomPosition).normalized;
            Vector3 right = (rightPosition - leftPosition).normalized;
            Vector3 normal = Vector3.Cross(forward, right).normalized;
            
            
            Quaternion pitchRotation = Quaternion.AngleAxis(PitchAngle, right);
            Quaternion yawRotation = Quaternion.AngleAxis(YawAngle, forward);
            normal = pitchRotation * normal;
            normal = yawRotation * normal;
            
            Vector3 anchorsCenter = (leftPosition + rightPosition + topPosition + bottomPosition) / 4.0f + CameraOffset;

            float pitchAngle = PitchAngle + 90;
            if (pitchAngle <= 0) pitchAngle = 1;
            if (pitchAngle >= 180) pitchAngle = 179;

            float yawAngle = YawAngle + 90;
            if (yawAngle <= 0) yawAngle = 1;
            if (yawAngle >= 180) yawAngle = 179;
            GetTargetParameters(bottomPosition, topPosition, GetVerticalFOV(), GetNearPlaneHeight(), pitchAngle, out float verticalLookPosition, out float verticalCameraDistane);
            GetTargetParameters(leftPosition, rightPosition, GetHorizontalFOV(), GetNearPlaneWidth(), yawAngle, out float horizontalLookPosition, out float horizontalCameraDistane);

            float distanceFromLookPoint = Mathf.Max(verticalCameraDistane, horizontalCameraDistane);

            float backOffset = 0f;
            if (horizontalCameraDistane > verticalCameraDistane)
            {
                float nearPlaneAngle = 90 + GetVerticalFOV() * 0.5f;
                float nearPlaneHalfDistance = GetNearPlaneHeight() * 0.5f;
                float bottomHalf = GetBottomOffset(horizontalCameraDistane, nearPlaneHalfDistance, nearPlaneAngle, pitchAngle);
                float bottomDist = (bottomPosition - topPosition).magnitude * 0.5f;
                if (bottomHalf > bottomDist)
                {
                    backOffset = (bottomHalf - bottomDist);
                }
            }
            
            Vector3 bottomLeft = bottomPosition - right * (rightPosition - leftPosition).magnitude * 0.5f;
            Vector3 lookPoint = bottomLeft + right * horizontalLookPosition + forward * verticalLookPosition;

            _targetNormal = -normal;
            Vector3 planeForward =  bottomPosition - topPosition;
            planeForward.Normalize();
            _targetPosition = lookPoint + normal * distanceFromLookPoint - planeForward * backOffset;
        }
        
        private void GetTargetParameters(Vector3 startPlanePoint, Vector3 endPlanePoint, float fov, float nearPlaneSize, float angle, out float cameraLookPosition, out float cameraDistance)
        {
            Vector3 forward = endPlanePoint - startPlanePoint;
            float distance = forward.magnitude;
            forward.Normalize();
            float nearPlaneAngle = 90 + fov * 0.5f;
            float topBeta = 360 - (270 + nearPlaneAngle - angle);
            float t = distance * Mathf.Sin(topBeta * Mathf.Deg2Rad) /
                      Mathf.Sin((nearPlaneAngle) * Mathf.Deg2Rad);
            float x1 = Mathf.Sin((180 - nearPlaneAngle)*Mathf.Deg2Rad)*(-nearPlaneSize*0.5f + t*0.5f)/Mathf.Sin((nearPlaneAngle-90.0f)*Mathf.Deg2Rad);
            float x2 = (t * 0.5f) * Mathf.Sin((90 - angle) * Mathf.Deg2Rad) /
                       Mathf.Sin(angle * Mathf.Deg2Rad);

            float h1 = t * 0.5f / Mathf.Sin(angle * Mathf.Deg2Rad);

            cameraLookPosition = h1;
            cameraDistance = x1 + x2;
        }

        private float GetVerticalFOV()
        {
            return CameraRig.Camera.fieldOfView;
        }

        private float GetHorizontalFOV()
        {
            float verticalFOV = GetVerticalFOV();
            float aspectRatio = CameraRig.Camera.aspect; // This is the width/height ratio of the camera's viewport
    
            // Convert vertical FOV from degrees to radians for calculation
            float verticalFOVRadians = Mathf.Deg2Rad * verticalFOV;
    
            // Use the formula to calculate horizontal FOV
            float horizontalFOVRadians = 2 * Mathf.Atan(Mathf.Tan(verticalFOVRadians / 2) * aspectRatio);
    
            // Convert the result back to degrees
            float horizontalFOV = Mathf.Rad2Deg * horizontalFOVRadians;

            return horizontalFOV;
        }
        private float GetAspectRatio()
        {
            return CameraRig.Camera.aspect;
        }
        
        private float GetNearPlaneDistance()
        {
            return CameraRig.Camera.nearClipPlane;
        }

        private float GetNearPlaneHeight()
        {
            return 2 * GetNearPlaneDistance() * Mathf.Tan(GetVerticalFOV() * 0.5f * Mathf.Deg2Rad);
        }

        private float GetNearPlaneWidth()
        {
            return GetNearPlaneHeight() * GetAspectRatio();
        }
        
        /// <summary>
        /// Only when horizontal distance is larger
        /// </summary>
        /// <returns></returns>
        private float GetBottomOffset(float cameraDistance, float nearPlaneHalfDistance, float nearPlaneAngle, float pitch)
        {
            float farPlaneInnerAngle = 270.0f - pitch - nearPlaneAngle;
            float t = Mathf.Sqrt(cameraDistance * cameraDistance + nearPlaneHalfDistance * nearPlaneHalfDistance);
            float p1 = Mathf.Asin(nearPlaneHalfDistance / t) * Mathf.Rad2Deg;
            float p2 = pitch - p1;
            float omega = 180.0f - p2 - farPlaneInnerAngle;
            float result = Mathf.Sin(omega * Mathf.Deg2Rad) * t / Mathf.Sin(farPlaneInnerAngle * Mathf.Deg2Rad);
            return result;


            // float sinNearPlaneAngle = Mathf.Sin((180.0f - nearPlaneAngle) * Mathf.Deg2Rad);
            // float sinPitch = Mathf.Sin((nearPlaneAngle - Mathf.Abs(pitch)) * Mathf.Deg2Rad);
            // float nearPlaneHeight = GetNearPlaneHeight();
            // float t = cameraDistance * sinPitch / sinNearPlaneAngle;
            // return t + nearPlaneHeight * 0.5f;
        }
    }
}