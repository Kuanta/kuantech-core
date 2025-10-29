using Kuantech.Core.Controller;
using Kuantech.Core.Utils;
using UnityEngine;

namespace Kuantech.Core.Camera
{
    public class FirstPersonCameraFollower : CameraFollower
    {
        //Runtime
        private PlayerController _playerController;
        private void Update()
        {
            if (_playerController == null || !CameraTarget) return;

            // Set target look direction from _playerController
            float yaw = _playerController.Yaw;
            float pitch = _playerController.Pitch;

            // Convert yaw/pitch to forward direction
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            TargetDirection = rot * Vector3.forward;
        }

        public void SetController(PlayerController controller)
        {
            _playerController = controller;
        }
    }
}