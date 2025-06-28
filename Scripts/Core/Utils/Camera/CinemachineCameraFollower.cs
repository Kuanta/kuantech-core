#if UNITY_6000
    using Unity.Cinemachine;
#else
    using Cinemachine;
#endif
using Kuantech.Core.Utils;
using UnityEngine;

namespace Kuantech.Core.Camera
{
    /// <summary>
    /// A camera follower that utilizes Cinemachine to follow a target.
    /// </summary>
    public class CinemachineCameraFollower : CameraFollower
    {
        [Header("Cinemachine")] [SerializeField]
        private CinemachineVirtualCamera CinemachineVirtualCamera;
        
        protected override void UpdatePosition(Vector3 targetPosition)
        {
        }

        protected override void UpdateRotation(Vector3 targetDirection)
        {
            
        }
    }
}