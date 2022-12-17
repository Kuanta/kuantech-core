using UnityEngine;

namespace Kuantech.UI
{
    public class CameraFacingBillboard : MonoBehaviour
    {
        public Camera camera;

        public void SetCamera(Camera camera)
        {
            this.camera = camera;
        }

        private void Start()
        {
            camera = Camera.main;
        }

        private void Update()
        {
            transform.forward = camera.transform.forward;
        }
    }
}