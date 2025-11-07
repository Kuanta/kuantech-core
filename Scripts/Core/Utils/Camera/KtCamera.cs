using DG.Tweening;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.Camera
{
    public class KtCamera : MonoBehaviour
    {
        public UnityEngine.Camera Camera;
        public GameObject Rig;

        [Header("FOV")]
        [SerializeField] private float BaseFOV = 60f;
        
        [Header("Camera Shake")] 
        public float ShakeDuration = 0.5f;
        public float ShakeStrength = 1.0f;
        public int Vibrato = 10;
        private float Randomness = 90.0f;

        [Header("Zoom")]
        [SerializeField] private GameObject ZoomAnchor;
        [SerializeField] private Vector3 ZoomInOffset;
        [SerializeField] private float ZoomSmoothDampTime = 0.1f;
        [SerializeField] private float ZoomFOV = 90f;
        
        //Zoom
        private float _targetFOV;
        private float _fovVel;
        private bool _zoomedIn;
        private Vector3 _zoomVel;

        private void Update()
        {
            //Zoom
            float currentFOV = Camera.fieldOfView;
            currentFOV = Mathf.SmoothDamp(currentFOV, GetTargetFOV(), ref _fovVel, ZoomSmoothDampTime);
            Camera.fieldOfView = currentFOV;

            ZoomAnchor.transform.localPosition = Vector3.SmoothDamp(ZoomAnchor.transform.localPosition,
                GetZoomRigOffset(), ref _zoomVel, ZoomSmoothDampTime);
        }
        
        #region Camera Effects
        /// <summary>
        /// Shakes with default values
        /// </summary>
        public void ShakeCamera()
        {
            ShakeCamera(ShakeStrength, ShakeDuration, Vibrato);
        }
        
        /// <summary>
        /// Shakes the camera with given parameters
        /// </summary>
        /// <param name="shakesStrength"></param>
        /// <param name="shakeDuration"></param>
        /// <param name="vibrato"></param>
        public void ShakeCamera(float shakesStrength, float shakeDuration, int vibrato)
        {
            if (Camera != null)
            {
                Camera.transform.DOKill();
                Camera.transform.DOShakePosition(shakeDuration, shakesStrength, vibrato, Randomness)
                    .OnComplete(() => Camera.transform.localPosition = Vector3.zero);
            }
        }
        #endregion

        #region Zoom

        public void ZoomIn()
        {
            _zoomedIn = true;
        }

        public void CancelZoomIn()
        {
            _zoomedIn = false;
        }
        
        private float GetTargetFOV()
        {
            if (_zoomedIn) return ZoomFOV;
            return BaseFOV;
        }

        private Vector3 GetZoomRigOffset()
        {
            if (_zoomedIn) return ZoomInOffset;
            return Vector3.zero;
        }

        #endregion

        #region World Check
        public Ray GetCenterRay()
        {
            return Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        }
        public bool RaycastWorld(float maxDistance, LayerMask layerMask, out RaycastHit hit)
        {
            Ray ray = GetCenterRay();
            return UnityEngine.Physics.Raycast(ray, out hit, maxDistance, layerMask, QueryTriggerInteraction.Ignore);
        }
        public Vector3 GetAimPoint(float maxDistance, LayerMask mask, out bool hitSomething, out GameObject hitObject, float hitRadius=0f)
        {
            var ray = GetCenterRay();
            if (hitRadius > 0)
            {
                if (SpherecastWorld(hitRadius, maxDistance, mask, out RaycastHit hit))
                {
                    hitSomething = true;
                    hitObject = hit.collider ? hit.collider.gameObject : null;
                    return hit.point;
                }
            }
            else
            {
                if (RaycastWorld(maxDistance, mask, out RaycastHit hit))
                {
                    hitSomething = true;
                    hitObject = hit.collider ? hit.collider.gameObject : null;
                    return hit.point;
                }
            }
    

            hitSomething = false;
            hitObject = null;
            return ray.origin + ray.direction * maxDistance;
        }
        
        /// <summary>
        /// For aim assist
        /// </summary>
        /// <param name="radius"></param>
        /// <param name="maxDistance"></param>
        /// <param name="mask"></param>
        /// <param name="hit"></param>
        /// <returns></returns>
        public bool SpherecastWorld(float radius, float maxDistance, LayerMask mask, out RaycastHit hit)
        {
            var ray = GetCenterRay();
            return UnityEngine.Physics.SphereCast(ray, radius, out hit, maxDistance, mask, QueryTriggerInteraction.Ignore);
        }
        #endregion
        public void SetCameraPosition(WorldPoint point)
        {
            transform.position = point.GetTargetPosition();
            transform.rotation = point.GetRotation();
        }
        
    }
}