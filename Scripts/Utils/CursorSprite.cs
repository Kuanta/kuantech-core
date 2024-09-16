using Kuantech.Utils;
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
        public GameObject Visual;
        private void Enable()
        {
            transform.position = GetCursorPosition();
            Visual.SetActive(false);
        }
        private void Update()
        {
            _targetPosition = GetCursorPosition();
            transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * FollowLerpFactor);
            if(Input.GetMouseButtonDown(0))
            {
                if (!Helpers.IsCursorOnUI()) Visual.SetActive(true);
               
            }
            else if(Input.GetMouseButtonUp(0))
            {
                Visual.SetActive(false);
            }
        }

        private Vector3 GetCursorPosition()
        {
            Vector3 mousePosition = Input.mousePosition;

            if (Camera == null)
            {
                //In overlay ui
                return mousePosition;
            }
            mousePosition.z = distanceFromCamera; // Set the distance from the camera
            return Camera.ScreenToWorldPoint(mousePosition);
        }

        private void OnMouseDown() {

        }

        private void OnMouseUp() {

        }
    }
}