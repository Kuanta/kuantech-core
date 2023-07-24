using UnityEngine;

namespace Kuantech.Core.Utils
{
    public class FrustrumAdjuster : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Camera cullingCamera;
        [SerializeField] private float extraFrustumCullRange = 10;
 
        private void Awake()
        {
            SetupCullingCamera();
        }
 
        private void SetupCullingCamera()
        {
            cullingCamera.gameObject.SetActive(false);
            cullingCamera.transform.SetParent(transform, false);
        }
 
        private void LateUpdate()
        {
            // Adjust field of view for frustum culling matrix to give some extra space so billboard sprites dont get culled while on screen
            cullingCamera.fieldOfView = mainCamera.fieldOfView + extraFrustumCullRange;
            mainCamera.cullingMatrix = cullingCamera.cullingMatrix;
        }
    }
}