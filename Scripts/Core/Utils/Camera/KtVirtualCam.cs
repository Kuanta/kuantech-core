using UnityEngine;

namespace Kuantech.Core.Camera
{
    /// <summary>
    /// A camera viewpoint marker -- no real Camera or AudioListener of its own. Whatever positions/rotates
    /// THIS transform (an OrbitCameraFollower, a PointFollower/ControllerRotationFollower rig, or just a
    /// static point) defines where the view should be; KtCamera hard-snaps its ONE real camera and one real
    /// AudioListener onto whichever KtVirtualCam is currently active -- no smoothing at that step, since
    /// whichever follower drives this transform already did whatever smoothing it wanted before this point.
    /// This is what lets FP/TP switching (and anything else that changes viewpoint) work as "which
    /// lightweight marker is active" instead of juggling multiple real Camera + AudioListener pairs and
    /// their enabled state.
    /// </summary>
    public class KtVirtualCam : MonoBehaviour
    {
        [Tooltip("Where the AudioListener should sit -- leave null to use this same transform.")]
        public Transform ListenerTransform;
    }
}
