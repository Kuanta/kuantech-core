using UnityEngine;

namespace Kuantech.Core.Utils
{
    public class ScreenSizeAdjuster : MonoBehaviour {
        public GameObject TopAnchor;
        public GameObject BottomAnchor;
        public GameObject LeftAnchor;
        public GameObject RightAnchor;

        public bool FitOnUpdate = true;

        private void Update()
        {
            if(!FitOnUpdate) return;
            FitCameraToAnchors();
        }
        public void FitCameraToAnchors()
        {
            Camera mainCamera = Camera.main;

            // Get the positions of the anchor points
            Vector3 topPosition = mainCamera.transform.InverseTransformPoint(TopAnchor.transform.localPosition);
            Vector3 leftPosition = mainCamera.transform.InverseTransformPoint(LeftAnchor.transform.localPosition);
            Vector3 rightPosition = mainCamera.transform.InverseTransformPoint(RightAnchor.transform.localPosition);
            Vector3 bottomPosition = mainCamera.transform.InverseTransformPoint(BottomAnchor.transform.localPosition);

            // Calculate the size of the camera's orthographic view
            float orthoSize = CalculateOrthographicSize(topPosition, leftPosition, rightPosition, bottomPosition);

            // Set the camera's orthographic size
            mainCamera.orthographicSize = orthoSize;

            // // Calculate the position for the camera to center on the anchor points
            // Vector3 targetLocalPosition = new Vector3((leftPosition.x + rightPosition.x) / 2f, (topPosition.y + bottomPosition.y) / 2f, mainCamera.transform.position.z);

            // // Set the camera's position
            // mainCamera.transform.position = transform.TransformPoint(targetLocalPosition);
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