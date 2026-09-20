using System;
using Kuantech.Core.Controller;
using UnityEngine;

namespace Kuantech.Core.Utils
{
    /// <summary>
    /// Generic "point this at wherever the player is looking" component -- reads yaw/pitch off the current
    /// PlayerController the same way FirstPersonCameraFollower/OrbitCameraFollower do, but drives its OWN
    /// transform instead of a dedicated camera. Pick which axis (or both) it should apply per instance.
    ///
    /// Typical use: drop this on a first-person rig's spine bone with ApplyYaw off and ApplyPitch on --
    /// yaw already matches the camera via AimHandler + InputHandler.AlwaysFaceCamera rotating the whole
    /// actor, so the bone only needs pitch layered on top of it. With Additive on (the default), that pitch
    /// is composed with whatever local rotation the Animator already gave the bone this frame, so any
    /// baked sway/bob/recoil animation keeps playing through untouched -- this only tilts it.
    /// </summary>
    public class ControllerRotationFollower : MonoBehaviour
    {
        [Header("Axes")]
        public bool ApplyYaw = false;
        public bool ApplyPitch = true;

        [Tooltip("On: composes the controller-driven rotation with whatever local rotation this transform " +
                 "already has this frame (e.g. from the Animator) -- use this on an animated bone so baked " +
                 "motion isn't overwritten, only tilted. Off: sets local rotation directly from yaw/pitch, " +
                 "discarding whatever else set it that frame -- use this on a plain, unanimated transform.")]
        public bool Additive = true;

        public bool InvertPitch = false;

        [Header("Smoothing (seconds)")]
        public float YawSmoothTime = 0.03f;
        public float PitchSmoothTime = 0.03f;

        [Header("Limits")]
        public float MinPitch = -89f;
        public float MaxPitch = 89f;

        [NonSerialized] public PlayerController Controller;
        [Tooltip("Same option as FirstPersonCameraFollower/OrbitCameraFollower: read the CONTROLLER'S " +
                 "already-smoothed TargetYaw/TargetPitch instead of its raw current Yaw/Pitch.")]
        public bool UseTargetValues = false;

        private float _currentYawAngle;
        private float _currentPitchAngle;
        private float _targetYawAngle;
        private float _targetPitchAngle;
        private float _yawVel;
        private float _pitchVel;

        private void LateUpdate()
        {
            Controller = ControllerManager.GetCurrentController();
            if (Controller == null) return;

            if (ApplyYaw)
                _targetYawAngle = Normalize360(UseTargetValues ? Controller.TargetYaw : Controller.Yaw);
            if (ApplyPitch)
            {
                float rawPitch = UseTargetValues ? Controller.TargetPitch : Controller.Pitch;
                _targetPitchAngle = Mathf.Clamp(rawPitch * (InvertPitch ? -1f : 1f), MinPitch, MaxPitch);
            }

            float dt = Mathf.Max(Time.deltaTime, 1e-6f);
            if (ApplyYaw)
                _currentYawAngle = Mathf.SmoothDampAngle(_currentYawAngle, _targetYawAngle, ref _yawVel, YawSmoothTime, Mathf.Infinity, dt);
            if (ApplyPitch)
            {
                _currentPitchAngle = Mathf.SmoothDampAngle(_currentPitchAngle, _targetPitchAngle, ref _pitchVel, PitchSmoothTime, Mathf.Infinity, dt);
                _currentPitchAngle = Mathf.Clamp(_currentPitchAngle, MinPitch, MaxPitch);
            }

            Quaternion controllerRot = Quaternion.Euler(ApplyPitch ? _currentPitchAngle : 0f, ApplyYaw ? _currentYawAngle : 0f, 0f);
            transform.localRotation = Additive ? transform.localRotation * controllerRot : controllerRot;
        }

        private static float Normalize360(float a) => (a % 360f + 360f) % 360f;
    }
}
