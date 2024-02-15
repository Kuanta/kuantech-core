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

            // Set the camera's orthographic size
            mainCamera.orthographicSize = orthoSize;

            //Set camera target position for perspective
            float cameraView = 2.0f * Mathf.Tan(0.5f * Mathf.Deg2Rad * mainCamera.fieldOfView);
            float distance = PerspectiveFactor * orthoSize / cameraView;
            distance += 0.5f * orthoSize;
            
            // Calculate the position for the camera to center on the anchor points
            Vector3 targetPosition = (leftPosition + rightPosition + topPosition + bottomPosition) / 4.0f - distance * mainCamera.transform.forward;

            // Set the camera's position
            mainCamera.transform.position = targetPosition + CameraOffset;
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
    }
}