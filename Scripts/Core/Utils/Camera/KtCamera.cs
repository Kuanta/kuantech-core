using DG.Tweening;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.Camera
{
    // Must run its LateUpdate after every ordinary follower (OrbitCameraFollower, ControllerRotationFollower,
    // PointFollower, ...) so the hard snap below in LateUpdate() always reads THIS frame's fresh virtual-cam
    // transform, never a stale one from before those followers ran -- Unity doesn't order different
    // MonoBehaviours' LateUpdate calls for you, this attribute is what actually guarantees it.
    [DefaultExecutionOrder(1000)]
    public class KtCamera : MonoBehaviour
    {
        public UnityEngine.Camera Camera;
        public GameObject Rig;

        [Header("Virtual Cameras")]
        [Tooltip("The ONE real AudioListener -- moved to whichever KtVirtualCam is active (or its own " +
                 "ListenerTransform, if it set one) every LateUpdate. Leave null if audio position doesn't " +
                 "matter for this game.")]
        [SerializeField] private AudioListener Listener;
        [Tooltip("Snapped to by SwitchToDefaultVirtualCam() -- normally the scene's third-person orbit point.")]
        [SerializeField] private KtVirtualCam DefaultVirtualCam;
        public KtVirtualCam ActiveVirtualCam { get; private set; }

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

        private void Awake()
        {
            if (ActiveVirtualCam == null) ActiveVirtualCam = DefaultVirtualCam;
        }

        private void Update()
        {
            if(ZoomAnchor == null) return;
            //Zoom
            float currentFOV = Camera.fieldOfView;
            currentFOV = Mathf.SmoothDamp(currentFOV, GetTargetFOV(), ref _fovVel, ZoomSmoothDampTime);
            Camera.fieldOfView = currentFOV;

            ZoomAnchor.transform.localPosition = Vector3.SmoothDamp(ZoomAnchor.transform.localPosition,
                GetZoomRigOffset(), ref _zoomVel, ZoomSmoothDampTime);
        }

        // Hard snap, no smoothing -- ActiveVirtualCam's OWN followers already smoothed however they wanted
        // to before this ran (see the DefaultExecutionOrder comment above for why this is guaranteed to run
        // last). NOTE: this overwrites Camera.transform.position outright, so the Zoom offset above (applied
        // to ZoomAnchor, a local child) only actually shows up if Camera reads its position FROM ZoomAnchor's
        // hierarchy rather than this script setting it directly -- verify zoom still visibly pushes in once
        // this is wired up; if it doesn't, the zoom offset needs to be folded into this method instead.
        private void LateUpdate()
        {
            if (ActiveVirtualCam == null || Camera == null) return;

            Transform vt = ActiveVirtualCam.transform;
            Camera.transform.SetPositionAndRotation(vt.position, vt.rotation);

            if (Listener != null)
            {
                Transform listenerTarget = ActiveVirtualCam.ListenerTransform != null ? ActiveVirtualCam.ListenerTransform : vt;
                Listener.transform.SetPositionAndRotation(listenerTarget.position, listenerTarget.rotation);
            }
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

        #region Virtual Cameras

        /// <summary>
        /// Switches viewpoint -- e.g. a Player's own FP virtual cam (a point under its Camera_Holder,
        /// already correctly positioned/rotated by ordinary Unity parenting, optionally with a
        /// PointFollower on it for bob) instead of the scene's default third-person orbit point. There's
        /// only ever ONE real Camera/AudioListener (see LateUpdate); this just changes what they snap to.
        /// </summary>
        public void SetActiveVirtualCam(KtVirtualCam virtualCam) => ActiveVirtualCam = virtualCam;

        public void SwitchToDefaultVirtualCam() => SetActiveVirtualCam(DefaultVirtualCam);

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
            return UnityEngine.Physics.Raycast(ray, out hit, maxDistance, layerMask, QueryTriggerInteraction.Collide);
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
            return UnityEngine.Physics.SphereCast(ray, radius, out hit, maxDistance, mask, QueryTriggerInteraction.Collide);
        }
        #endregion
        public void SetCameraPosition(WorldPoint point)
        {
            transform.position = point.GetTargetPosition();
            transform.rotation = point.GetRotation();
        }
        
    }
}