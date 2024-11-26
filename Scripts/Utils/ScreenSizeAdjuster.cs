using DG.Tweening;
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
        public Camera CameraToFit;

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
            if (CameraToFit == null) return;
            CameraToFit.transform.DOMove(_targetPosition, AnimationDuration).OnComplete(() =>
            {
                onReached?.Invoke();
            }).SetEase(EaseAnimationCurve);
            Quaternion targetRot = Quaternion.LookRotation(_targetNormal, Vector3.up);
            CameraToFit.transform.DORotate(targetRot.eulerAngles, duration);
        }
        
        public void Fit()
        {
            if (CameraToFit == null) return;
            CameraToFit.transform.forward = _targetNormal;
            CameraToFit.transform.position = _targetPosition;
        }

        private void CalculateTargetParameters()
        {
            if (CameraToFit == null) return;
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
            Vector3 bottomLeft = bottomPosition - right * (rightPosition - leftPosition).magnitude * 0.5f;
            Vector3 lookPoint = bottomLeft + right * horizontalLookPosition + forward * verticalLookPosition;

            _targetNormal = -normal;
            _targetPosition = lookPoint + normal * distanceFromLookPoint;
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
            return CameraToFit.fieldOfView;
        }

        private float GetHorizontalFOV()
        {
            float verticalFOV = GetVerticalFOV();
            float aspectRatio = CameraToFit.aspect; // This is the width/height ratio of the camera's viewport
    
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
            return CameraToFit.aspect;
        }
        
        private float GetNearPlaneDistance()
        {
            return CameraToFit.nearClipPlane;
        }

        private float GetNearPlaneHeight()
        {
            return 2 * GetNearPlaneDistance() * Mathf.Tan(GetVerticalFOV() * 0.5f * Mathf.Deg2Rad);
        }

        private float GetNearPlaneWidth()
        {
            return GetNearPlaneHeight() * GetAspectRatio();
        }
        
        // public void FitCameraToAnchors()
        // {
        //     Camera mainCamera = Camera.main;
        //
        //     // Get the positions of the anchor points
        //     Vector3 topPosition = TopAnchor.transform.position;
        //     Vector3 leftPosition = LeftAnchor.transform.position;
        //     Vector3 rightPosition = RightAnchor.transform.position;
        //     Vector3 bottomPosition = BottomAnchor.transform.position;
        //
        //     // Calculate the size of the camera's orthographic view
        //     float orthoSize = CalculateOrthographicSize(topPosition, leftPosition, rightPosition, bottomPosition);
        //     float distance = CalculatePerspectiveDistance(topPosition, leftPosition, rightPosition, bottomPosition);
        //     // Set the camera's orthographic size
        //     mainCamera.orthographicSize = orthoSize;
        //
        //     // Calculate the position for the camera to center on the anchor points
        //     Vector3 anchorsCenter = (leftPosition + rightPosition + topPosition + bottomPosition) / 4.0f + CameraOffset;
        //     
        //     Vector3 viewPlaneNormal = GetViewPlaneNormal();
        //     Vector3 targetPosition = anchorsCenter + distance * viewPlaneNormal;
        //
        //     // Set the camera's position
        //     mainCamera.transform.position = targetPosition;
        //     mainCamera.transform.LookAt(anchorsCenter);
        // }

        // private Vector3 GetViewPlaneNormal()
        // {
        //     Vector3 forward = TopAnchor.transform.position - BottomAnchor.transform.position;
        //     Vector3 right = RightAnchor.transform.position - LeftAnchor.transform.position;
        //     Vector3 normal = Vector3.Cross(forward, right);
        //     return normal.normalized;
        // }
    //     private float CalculateOrthographicSize(Vector3 top, Vector3 left, Vector3 right, Vector3 bottom)
    //     {
    //         Camera mainCamera = Camera.main;
    //         float aspectRatio = mainCamera.aspect;
    //
    //         float reqHeight = Vector3.Distance(top, bottom);
    //         float reqWidth = Vector3.Distance(left, right);
    //
    //         float orthSizeForHeight = reqHeight / 2.0f;
    //         float ortSizeForWidth = reqWidth / aspectRatio * 0.5f;
    //         return Mathf.Max(orthSizeForHeight, ortSizeForWidth);
    //     }
    //
    //     private float CalculatePerspectiveDistance(Vector3 top, Vector3 left, Vector3 right, Vector3 bottom)
    //     {
    //         Camera mainCamera = Camera.main;
    //         float aspectRatio = mainCamera.aspect;
    //         float fov = mainCamera.fieldOfView;
    //         float fovRad = fov * Mathf.Deg2Rad;
    //
    //         float viewPlaneHeight = Vector3.Distance(top, bottom);
    //         float viewPlaneWidth = Vector3.Distance(left, right);
    //
    //         // Calculate the distance from the camera to fit the rectangle into view
    //         float distanceWidth = viewPlaneWidth / (2f * Mathf.Tan(fovRad / 2f) * aspectRatio);
    //         float distanceHeight = viewPlaneHeight / (2f * Mathf.Tan(fovRad / 2f));
    //
    //         // Take the maximum of the two distances
    //         float distance = Mathf.Max(distanceWidth, distanceHeight);
    //         return distance;
    //     }
    //
    }
}