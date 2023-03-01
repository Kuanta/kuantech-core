using UnityEngine;
using UnityEngine.Serialization;

namespace Kuantech.UI
{
    public class CameraFacingBillboard : MonoBehaviour
    {
        [FormerlySerializedAs("camera")] public Camera MainCamera;

        public void SetCamera(Camera camera)
        {
            MainCamera = camera;
        }

        private void Start()
        {
            MainCamera = Camera.main;
        }

        private void Update()
        {
            transform.forward = MainCamera.transform.forward;
        }
    }
}