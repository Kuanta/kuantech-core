using UnityEngine;

namespace Kuantech.Core
{
    public class SubCamera : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera MainCamera;
        [SerializeField] private UnityEngine.Camera SubCam; 
        [SerializeField] private bool SyncFOV;
        [SerializeField] private bool SyncOrthographicSize = true;

        private void LateUpdate()
        {
            if (MainCamera == null || SubCam == null) return;
            if (SyncFOV)
            {
                SubCam.fieldOfView = MainCamera.fieldOfView;
            }

            if (SyncOrthographicSize)
            {
                SubCam.orthographicSize = MainCamera.orthographicSize;
            }
        }
    }
}