using UnityEngine;

namespace Kuantech.Core.Utils
{
    /// <summary>
    /// Generic constraint-like follower: snaps/eases this transform's position and/or rotation onto Target
    /// (plus an optional offset), each axis toggleable independently with its own smoothing. No knowledge of
    /// controllers, actors, or cameras -- just "sit where Target is." Typical use: the FirstPersonCamera
    /// GameObject (a static child of Camera_Holder, already correctly yaw/pitched by ordinary Unity
    /// parenting) gets one of these pointed at the FP rig's Camera_Bone so whatever bob/sway/recoil is baked
    /// into the rig's animation carries the camera's position along -- safe because Camera_Bone is a SIBLING
    /// of the camera under Camera_Holder, never a descendant of it, so there's nothing for the camera's own
    /// movement to feed back into.
    /// </summary>
    public class PointFollower : MonoBehaviour
    {
        public Transform Target;

        [Header("Position")]
        public bool FollowPosition = true;
        [Tooltip("In Target's local space -- moves and rotates with it.")]
        public Vector3 PositionOffset = Vector3.zero;
        [Tooltip("0 snaps to Target exactly, every frame. Raise it for a softer, laggier follow.")]
        public float PositionSmoothTime = 0f;
        public float MaxFollowSpeed = Mathf.Infinity;

        [Header("Rotation")]
        public bool FollowRotation = true;
        [Tooltip("Applied on top of Target's rotation, in Target's local space.")]
        public Vector3 RotationOffset = Vector3.zero;
        [Tooltip("0 snaps to Target exactly, every frame. Raise it for a softer, laggier follow.")]
        public float RotationSmoothTime = 0f;

        private Vector3 _positionVel;

        private void LateUpdate()
        {
            if (Target == null) return;
            float dt = Mathf.Max(Time.deltaTime, 1e-6f);

            if (FollowPosition)
            {
                Vector3 targetPosition = Target.TransformPoint(PositionOffset);
                transform.position = PositionSmoothTime <= 0f
                    ? targetPosition
                    : Vector3.SmoothDamp(transform.position, targetPosition, ref _positionVel, PositionSmoothTime, MaxFollowSpeed, dt);
            }

            if (FollowRotation)
            {
                Quaternion targetRotation = Target.rotation * Quaternion.Euler(RotationOffset);
                transform.rotation = RotationSmoothTime <= 0f
                    ? targetRotation
                    // Frame-rate-independent exponential ease -- Slerp doesn't have a direct SmoothDamp
                    // equivalent, this is the standard substitute (bigger RotationSmoothTime = slower catch-up).
                    : Quaternion.Slerp(transform.rotation, targetRotation, 1f - Mathf.Exp(-dt / RotationSmoothTime));
            }
        }
    }
}
