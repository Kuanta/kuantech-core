using System;
using Kuantech.Core.Camera;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.Utils
{
    /// <summary>
    /// Represents a camera target defined by a rectangle
    /// </summary>
    public class FrameFitterCameraTarget : CameraTarget
    {
        [Header("Anchors")]
        public GameObject TopAnchor;
        public GameObject BottomAnchor;
        public GameObject LeftAnchor;
        public GameObject RightAnchor;
        
        
        [Header("Angles")] 
        public float PitchAngle;
        public float YawAngle;

        [NonSerialized] public KtCamera KtCamera;
        
        public Vector3 CameraOffset;
        public float BottomAnchorFactor = 1.0f;

        private WorldPoint _targetPoint;
        private void Update()
        {
            _targetPoint = GetTargetPoint();
        }
        public override Vector3 GetTargetPosition()
        {
            if (_targetPoint == null)
            {
                return Vector3.zero;
            }
            return _targetPoint.Position;
        }

        public override Quaternion GetTargetRotation()
        {
            if (_targetPoint == null)
            {
                return Quaternion.identity;
            }
            return _targetPoint.Rotation;
        }

        public override float GetTargetOrthographicSize()
        {
            if (_targetPoint == null) return base.GetTargetOrthographicSize();
            return _targetPoint.OrthographicSize;
        }
        
        public bool IsCameraOrthographic()
        {
            return CameraManager.GetCamera().orthographic;
        }
        
        public WorldPoint GetTargetPoint()
        {
            WorldPoint targetPoint = new WorldPoint();
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
            
            if (IsCameraOrthographic())
            {

                normal = pitchRotation * yawRotation * normal;

                Quaternion rotation = Quaternion.LookRotation(-normal, Vector3.up);

                // Anchor'ların projection düzlemiyle doğru boyutlarını al
                float width = Vector3.ProjectOnPlane(rightPosition - leftPosition, normal).magnitude;
                float height = Vector3.ProjectOnPlane(topPosition - bottomPosition, normal).magnitude;

                float aspect = GetAspectRatio();

                // Hem dikey hem yatay sığması için gereken en büyük size
                float orthoSizeToFitHeight = height / 2f;
                float orthoSizeToFitWidth = (width / aspect) / 2f;
                float requiredOrthoSize = Mathf.Max(orthoSizeToFitHeight, orthoSizeToFitWidth);
                
                return new WorldPoint
                {
                    Position = anchorsCenter,
                    Rotation = rotation,
                    OrthographicSize = requiredOrthoSize
                };
            }

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
            //If screen is thinner...
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

            Vector3 planeForward =  bottomPosition - topPosition;
            planeForward.Normalize();
            Vector3 targetPosition = lookPoint + normal * distanceFromLookPoint - planeForward * backOffset * BottomAnchorFactor;
            
            targetPoint.Position = targetPosition;
            targetPoint.Rotation = Quaternion.LookRotation(-normal, Vector3.up);
            return targetPoint;
        }

        private float GetAspectRatio()
        {
            return CameraManager.GetCamera().aspect;
        }
        private float GetNearPlaneWidth()
        {
            return GetNearPlaneHeight() * GetAspectRatio();
        }
        private float GetVerticalFOV()
        {
            return CameraManager.GetCamera().fieldOfView;
        }

        private float GetHorizontalFOV()
        {
            float verticalFOV = GetVerticalFOV();
            float aspectRatio = CameraManager.GetCamera().aspect; // This is the width/height ratio of the camera's viewport
    
            // Convert vertical FOV from degrees to radians for calculation
            float verticalFOVRadians = Mathf.Deg2Rad * verticalFOV;
    
            // Use the formula to calculate horizontal FOV
            float horizontalFOVRadians = 2 * Mathf.Atan(Mathf.Tan(verticalFOVRadians / 2) * aspectRatio);
    
            // Convert the result back to degrees
            float horizontalFOV = Mathf.Rad2Deg * horizontalFOVRadians;

            return horizontalFOV;
        }
        private float GetNearPlaneDistance()
        {
            return CameraManager.GetCamera().nearClipPlane;
        }
        private float GetNearPlaneHeight()
        {
            return 2 * GetNearPlaneDistance() * Mathf.Tan(GetVerticalFOV() * 0.5f * Mathf.Deg2Rad);
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