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

        private void Awake()
        {
            MainCamera = Camera.main;
        }

        private void OnEnable()
        {
            transform.forward = MainCamera.transform.forward * Direction;
        }

        private void Update()
        {
            transform.forward = MainCamera.transform.forward * Direction;
        }
    }
}