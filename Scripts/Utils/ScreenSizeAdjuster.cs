using UnityEngine;

namespace Kuantech.Core.Utils
{
    public class ScreenSizeAdjuster : MonoBehaviour {
        [Header("Anchors")]
        public GameObject TopAnchor;
        public GameObject BottomAnchor;
        public GameObject LeftAnchor;
        public GameObject RightAnchor;
        public Vector3 CameraOffset;

        [Header("Perspective")]
        public float PerspectiveFactor = 2.0f;
        public bool FitOnUpdate = true;

        protected virtual void Update()
        {
            if(!FitOnUpdate) return;
            FitCameraToAnchors();
        }
        public void FitCameraToAnchors()
        {
            Camera mainCamera = Camera.main;

            // Get the positions of the anchor points
            Vector3 topPosition = TopAnchor.transform.position;
            Vector3 leftPosition = LeftAnchor.transform.position;
            Vector3 rightPosition = RightAnchor.transform.position;
            Vector3 bottomPosition = BottomAnchor.transform.position;

            // Calculate the size of the camera's orthographic view
            float orthoSize = CalculateOrthographicSize(topPosition, leftPosition, rightPosition, bottomPosition);
            float distance = CalculatePerspectiveDistance(topPosition, leftPosition, rightPosition, bottomPosition);
            // Set the camera's orthographic size
            mainCamera.orthographicSize = orthoSize;

            // Calculate the position for the camera to center on the anchor points
            Vector3 anchorsCenter = (leftPosition + rightPosition + topPosition + bottomPosition) / 4.0f + CameraOffset;
            
            Vector3 viewPlaneNormal = GetViewPlaneNormal();
            Vector3 targetPosition = anchorsCenter + distance * viewPlaneNormal;

            // Set the camera's position
            mainCamera.transform.position = targetPosition;
            mainCamera.transform.LookAt(anchorsCenter);
        }

        private Vector3 GetViewPlaneNormal()
        {
            Vector3 forward = TopAnchor.transform.position - BottomAnchor.transform.position;
            Vector3 right = RightAnchor.transform.position - LeftAnchor.transform.position;
            Vector3 normal = Vector3.Cross(forward, right);
            return normal.normalized;
        }
        private float CalculateOrthographicSize(Vector3 top, Vector3 left, Vector3 right, Vector3 bottom)
        {
            Camera mainCamera = Camera.main;
            float aspectRatio = mainCamera.aspect;

            float reqHeight = Vector3.Distance(top, bottom);
            float reqWidth = Vector3.Distance(left, right);

            float orthSizeForHeight = reqHeight / 2.0f;
            float ortSizeForWidth = reqWidth / aspectRatio * 0.5f;
            return Mathf.Max(orthSizeForHeight, ortSizeForWidth);
        }

        private float CalculatePerspectiveDistance(Vector3 top, Vector3 left, Vector3 right, Vector3 bottom)
        {
            Camera mainCamera = Camera.main;
            float aspectRatio = mainCamera.aspect;
            float fov = mainCamera.fieldOfView;
            float fovRad = fov * Mathf.Deg2Rad;

            float viewPlaneHeight = Vector3.Distance(top, bottom);
            float viewPlaneWidth = Vector3.Distance(left, right);

            // Calculate the distance from the camera to fit the rectangle into view
            float distanceWidth = viewPlaneWidth / (2f * Mathf.Tan(fovRad / 2f) * aspectRatio);
            float distanceHeight = viewPlaneHeight / (2f * Mathf.Tan(fovRad / 2f));

            // Take the maximum of the two distances
            float distance = Mathf.Max(distanceWidth, distanceHeight);
            return distance;
        }

    }
}