using UnityEngine;

namespace Kuantech.Core.Utils
{
    /// <summary>
    /// Smoothly follows a CameraRig by position, and looks toward the rig's LookTarget.
    /// Used for cutscenes: enable this, disable IsometricCameraFollower.
    /// </summary>
    public class RigFollower : MonoBehaviour
    {
        public CameraRig Rig;
        public float PositionSmoothTime = 0.1f;
        public float RotationSmoothTime = 0.1f;
        public GameObject CameraObject;

        private Vector3 _posVel;

        private void Awake() => enabled = false;

        private void LateUpdate()
        {
            if (Rig == null) return;

            Transform t = CameraObject != null ? CameraObject.transform : transform;

            t.position = Vector3.SmoothDamp(
                t.position, Rig.Position, ref _posVel, PositionSmoothTime);

            t.rotation = Quaternion.Slerp(
                t.rotation,
                Rig.GetLookRotation(),
                Time.deltaTime / Mathf.Max(RotationSmoothTime, 1e-4f));
        }

        public void Follow(CameraRig rig)
        {
            Rig = rig;
            Transform t = CameraObject != null ? CameraObject.transform : transform;
            t.SetPositionAndRotation(rig.Position, rig.GetLookRotation());
            _posVel = Vector3.zero;
            enabled = true;
        }

        public void Stop() => enabled = false;
    }
}
