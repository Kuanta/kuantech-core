using Kuantech.Core.Controller;
using UnityEngine;

namespace Kuantech.Core.Camera
{
    /// <summary>
    /// A target for the Cinemachine Orbit Camera to follow and orbit around.
    /// Suited for third-person camera setups.
    /// </summary>
    public class CinemachineOrbitCameraTarget : MonoBehaviour
    {
        public float PitchAngle;
        public float YawAngle;
        public PlayerController Controller;
        public bool UseControllerYawPitch = false;
        
            
        private void Update()
        {
            if (UseControllerYawPitch)
            {
                Controller = ControllerManager.GetCurrentController();
                SetYaw(Controller.Yaw);
                SetPitch(Controller.Pitch);
            }
        }
    
        private void LateUpdate()
        {
            transform.rotation = GetTargetRotation();
        }
        
        public void SetPitch(float pitch, bool immediate = false)
        {
            PitchAngle = pitch;
        }
        
        public void SetYaw(float yaw, bool immediate = false)
        {
            YawAngle = yaw;
        }
        
        public Quaternion GetTargetRotation()
        {
            if (Controller != null) return Controller.GetRotation();
            return Quaternion.Euler(PitchAngle, YawAngle, 0f);
        }
    }

}