using UnityEngine;

namespace Kuantech.Core.Utils
{
    public class CursorSprite : MonoBehaviour
    {
        public Camera Camera;

        [SerializeField]
        private float distanceFromCamera = 10f;
        public float FollowLerpFactor = 10f;
        private Vector3 _targetPosition;
    
        private void Enable()
        {
            transform.position = GetCursorPosition();
        }
        private void Update()
        {
            _targetPosition = GetCursorPosition();
            transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * FollowLerpFactor);
        }

        private Vector3 GetCursorPosition()
        {
            Vector3 mousePosition = Input.mousePosition;
            mousePosition.z = distanceFromCamera; // Set the distance from the camera
            return Camera.ScreenToWorldPoint(mousePosition);
        }
    }
}