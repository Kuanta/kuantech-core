using System;
using Kuantech.Core.Camera;
using Kuantech.Core.Controller;
using UnityEngine;

namespace Kuantech.Core.Utils
{
    /// <summary>
    /// First-person camera: sits at Anchor (the actor's eye/head point) and looks straight along the
    /// yaw/pitch direction. No orbit radius and no look-at-from-outside math like OrbitCameraFollower —
    /// the camera IS the head, it doesn't orbit one.
    ///
    /// Position tracking is intentionally tight (PositionSmoothTime defaults near-zero): any lag between the
    /// head anchor and the camera reads as disorienting in first person, unlike third person where a little
    /// lag looks cinematic. Rotation gets a small amount of smoothing for a soft, non-twitchy feel without
    /// adding perceptible input lag.
    /// </summary>
    public class FirstPersonCameraFollower : MonoBehaviour
    {
        // Scene has exactly one active follow camera per client — bind Anchor here the same way
        // OrbitCameraFollower/IsometricCameraFollower do, whichever view mode is currently active.
        public static FirstPersonCameraFollower Instance { get; private set; }

        public KtCamera Camera;
        public Transform Anchor;

        [Tooltip("Offset from Anchor, in Anchor's local space — moves and rotates with it (e.g. a head bone's own animation/tilt carries the eye point along).")]
        public Vector3 AnchorOffset = Vector3.zero;
        public bool InvertY = false;

        [Header("Angle Smoothing (seconds)")]
        [Tooltip("Keep small — this is felt as input lag, unlike OrbitCameraFollower's smoothing which mostly reads as camera weight.")]
        public float YawSmoothTime = 0.03f;
        public float PitchSmoothTime = 0.03f;

        [Header("Position Smoothing")]
        [Tooltip("Keep at 0 (or tiny) — any lag between the head anchor and the camera is disorienting in first person. 0 snaps the camera to the anchor exactly, every frame.")]
        public float PositionSmoothTime = 0f;
        public float MaxFollowSpeed = Mathf.Infinity;

        [Header("Limits")]
        public float MinPitch = -89f;
        public float MaxPitch = 89f;

        private float _currentYawAngle;
        private float _currentPitchAngle;

        [SerializeField] private float _targetYawAngle;
        [SerializeField] private float _targetPitchAngle;

        [NonSerialized] public PlayerController Controller;
        [Tooltip("Same option as OrbitCameraFollower: read Yaw/Pitch from the current PlayerController (fed by PlayerInputHandler's AddYaw/AddPitch) instead of relying on SetYaw/SetPitch being called directly.")]
        public bool UseControllerYawPitch = true;
        public bool UseTargetValues = false;

        private float _yawVel;
        private float _pitchVel;
        private Vector3 _positionVel;

        private void Awake()
        {
            Instance = this;
            if (Camera == null) Camera = CameraManager.GetKtCamera();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private Transform GetTransformToUpdate() => Camera ? Camera.transform : transform;

        private void LateUpdate()
        {
            if (Anchor == null) return;

            if (UseControllerYawPitch)
            {
                Controller = ControllerManager.GetCurrentController();
                if (Controller != null)
                {
                    float targetYaw = Normalize360(UseTargetValues ? Controller.TargetYaw : Controller.Yaw);
                    float targetPitch = Mathf.Clamp(
                        UseTargetValues ? Controller.TargetPitch : Controller.Pitch * (InvertY ? -1f : 1f),
                        MinPitch, MaxPitch);
                    SetYaw(targetYaw);
                    SetPitch(targetPitch);
                }
            }

            float dt = Mathf.Max(Time.deltaTime, 1e-6f);

            _currentYawAngle = Mathf.SmoothDampAngle(
                _currentYawAngle, _targetYawAngle, ref _yawVel, YawSmoothTime, Mathf.Infinity, dt);
            _currentPitchAngle = Mathf.SmoothDampAngle(
                _currentPitchAngle, _targetPitchAngle, ref _pitchVel, PitchSmoothTime, Mathf.Infinity, dt);
            _currentPitchAngle = Mathf.Clamp(_currentPitchAngle, MinPitch, MaxPitch);

            Transform t = GetTransformToUpdate();
            Vector3 targetPosition = Anchor.TransformPoint(AnchorOffset);

            // Reads and writes the same transform (t) — OrbitCameraFollower's LateUpdate has the full story
            // on why that matters: mixing this script's own (often stationary) transform with the driven
            // camera's transform is what caused its wobble.
            t.position = PositionSmoothTime <= 0f
                ? targetPosition
                : Vector3.SmoothDamp(t.position, targetPosition, ref _positionVel, PositionSmoothTime, MaxFollowSpeed, dt);

            t.rotation = Quaternion.Euler(_currentPitchAngle, _currentYawAngle, 0f);
        }

        public void SetYaw(float yaw, bool immediate = false)
        {
            _targetYawAngle = yaw;
            if (immediate) { _currentYawAngle = yaw; _yawVel = 0f; }
        }

        public void SetPitch(float pitch, bool immediate = false)
        {
            _targetPitchAngle = Mathf.Clamp(pitch, MinPitch, MaxPitch);
            if (immediate) { _currentPitchAngle = _targetPitchAngle; _pitchVel = 0f; }
        }

        private static float Normalize360(float a) => (a % 360f + 360f) % 360f;
    }
}
