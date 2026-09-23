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

        [Tooltip("Degrees of angular kick per unit of shake strength. First person lives on this one: a " +
                 "positional shake big enough to read from inside the head looks like the whole world is " +
                 "sliding, while a rotational one reads as being hit. Third person is the opposite, so drop " +
                 "this and raise the strength there.")]
        public float ShakeAngularPerUnit = 20f;

        // Shake state. Held as an OFFSET rather than written onto the camera transform -- see LateUpdate.
        private float _shakeTimeLeft;
        private float _shakeTotalDuration;
        private float _shakeStrength;
        private int _shakeVibrato;
        private Vector2 _shakeSeed;
        private Vector3 _shakeOffset;
        private Vector3 _shakeEuler;

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

            UpdateShake(Time.unscaledDeltaTime);

            Transform vt = ActiveVirtualCam.transform;

            // The shake is added ON TOP of the snap, in the virtual cam's own space, and that is the whole
            // point of it living here. The snap overwrites the camera transform outright every frame, so a
            // shake that animates that same transform -- which is what the old DOShakePosition did -- is
            // erased before it is ever drawn. It looked like the shake "stopped working" when virtual cams
            // arrived; it had simply lost the argument over who writes the transform last.
            Quaternion rotation = vt.rotation;
            Vector3 position = vt.position + rotation * _shakeOffset;
            if (_shakeEuler != Vector3.zero) rotation *= Quaternion.Euler(_shakeEuler);

            Camera.transform.SetPositionAndRotation(position, rotation);

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
            if (shakesStrength <= 0f || shakeDuration <= 0f) return;

            // A new shake replaces the one in flight instead of adding to it. Being hit by two zombies at
            // once should not compound into a wobble that outlasts both blows, and restarting a 0.2s shake
            // is not something the eye can pick out anyway.
            _shakeTotalDuration = shakeDuration;
            _shakeTimeLeft = shakeDuration;
            _shakeStrength = shakesStrength;
            _shakeVibrato = Mathf.Max(1, vibrato);
            _shakeSeed = new Vector2(UnityEngine.Random.value * 1000f, UnityEngine.Random.value * 1000f);
        }

        /// <summary>
        /// Unscaled time on purpose: a hit stop (see HitStopFxBehaviour) freezes the world precisely when
        /// something has just landed, which is exactly when the shake should still be moving.
        /// </summary>
        private void UpdateShake(float deltaTime)
        {
            if (_shakeTimeLeft <= 0f)
            {
                _shakeOffset = Vector3.zero;
                _shakeEuler = Vector3.zero;
                return;
            }

            _shakeTimeLeft -= deltaTime;
            float remaining = Mathf.Clamp01(_shakeTimeLeft / Mathf.Max(0.0001f, _shakeTotalDuration));
            float damper = remaining * remaining; // eases out, and lands on exactly zero
            float phase = (_shakeTotalDuration - _shakeTimeLeft) * _shakeVibrato;

            // Smooth noise rather than a fresh random each frame: per-frame randomness turns into buzzing at
            // high frame rates and shakes visibly harder on a 144Hz screen than on a 60Hz one. Perlin is
            // sampled by TIME, so it reads the same on both.
            float x = Mathf.PerlinNoise(_shakeSeed.x + phase, 0f) * 2f - 1f;
            float y = Mathf.PerlinNoise(_shakeSeed.y + phase, 0f) * 2f - 1f;

            float amount = _shakeStrength * damper;
            _shakeOffset = new Vector3(x, y, 0f) * amount;
            _shakeEuler = new Vector3(-y, x, x * 0.5f) * (amount * ShakeAngularPerUnit);
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