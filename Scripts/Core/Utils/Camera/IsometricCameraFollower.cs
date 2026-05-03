using Kuantech.Core.Camera;
using UnityEngine;

namespace Kuantech.Core.Utils
{
    public class IsometricCameraFollower : MonoBehaviour
    {
        public KtCamera Camera;
        public Transform Anchor;
        public Vector3 AnchorOffset = Vector3.zero;

        [Header("Angle")]
        [SerializeField] private float _pitch    = 45f;
        [SerializeField] private float _targetYaw = 45f;
        public float Distance = 15f;

        [Header("Yaw Rotation")]
        public bool  SnapYaw      = true;
        public float YawSnapStep  = 90f;
        public float YawSmoothTime = 0.15f;

        [Header("Position Smoothing")]
        public float PositionSmoothTime = 0.12f;
        public float MaxFollowSpeed     = Mathf.Infinity;

        public float Yaw   => _currentYaw;
        public float Pitch => _pitch;

        private float   _currentYaw;
        private float   _yawVel;
        private Vector3 _positionVel;

        private void Awake()
        {
            _currentYaw = _targetYaw;
        }

        private void LateUpdate()
        {
            if (Anchor == null) return;
            float dt = Mathf.Max(Time.deltaTime, 1e-6f);

            _currentYaw = Mathf.SmoothDampAngle(
                _currentYaw, _targetYaw, ref _yawVel, YawSmoothTime, Mathf.Infinity, dt);

            Transform t  = GetTransformToUpdate();
            Vector3 newPos = Vector3.SmoothDamp(
                t.position, GetTargetPosition(), ref _positionVel, PositionSmoothTime, MaxFollowSpeed, dt);
            t.position = newPos;

            // Rotation computed from actual new position so it stays in sync with where the camera is.
            Vector3 toAnchor = GetAnchorWithOffset() - newPos;
            if (toAnchor.sqrMagnitude >= 1e-8f)
                t.rotation = Quaternion.LookRotation(toAnchor.normalized, Vector3.up);
        }

        // ── Setters ───────────────────────────────────────────────────────────

        public void SetYaw(float yaw, bool immediate = false)
        {
            _targetYaw = yaw;
            if (immediate) { _currentYaw = yaw; _yawVel = 0f; }
        }

        public void SetPitch(float pitch, bool immediate = false)
        {
            _pitch = pitch;
        }

        /// <summary>Rotates yaw by delta degrees, snapping if SnapYaw is enabled.</summary>
        public void RotateYaw(float delta)
        {
            float newYaw = _targetYaw + delta;
            if (SnapYaw) newYaw = Mathf.Round(newYaw / YawSnapStep) * YawSnapStep;
            SetYaw(newYaw);
        }

        // ── Position / Rotation ───────────────────────────────────────────────

        public Vector3 GetTargetPosition()
        {
            float pitchRad = Mathf.Deg2Rad * _pitch;
            float yawRad   = Mathf.Deg2Rad * _currentYaw;
            float cp       = Mathf.Cos(pitchRad);

            // Spherical coordinates → cartesian
            Vector3 offset = new Vector3(
                Distance * cp * Mathf.Sin(yawRad),
                Distance * Mathf.Sin(pitchRad),
                Distance * cp * Mathf.Cos(yawRad)
            );

            return GetAnchorWithOffset() + offset;
        }

        public Quaternion GetTargetRotation()
        {
            Vector3 toAnchor = GetAnchorWithOffset() - GetTargetPosition();
            if (toAnchor.sqrMagnitude < 1e-8f) return transform.rotation;
            return Quaternion.LookRotation(toAnchor.normalized, Vector3.up);
        }

        public Vector3 GetAnchorWithOffset() => Anchor ? Anchor.position + AnchorOffset : transform.position;

        private Transform GetTransformToUpdate() => Camera ? Camera.transform : transform;
    }
}
