using Kuantech.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace Kuantech.UI
{
    public class CameraFacingBillboard : MonoBehaviour
    {
        [FormerlySerializedAs("camera")] public Camera MainCamera;
        [SerializeField] private int Direction = 1;
        public void SetCamera(Camera camera)
        {
            MainCamera = camera;
        }

        private void Start()
        {
            MainCamera = Camera.main;
        }

        private void OnEnable()
        {
            if (MainCamera == null) return;
            transform.forward = MainCamera.transform.forward * Direction;
        }

        private void Update()
        {
            if (MainCamera == null) return;
            transform.forward = MainCamera.transform.forward * Direction;
        }
    }
}