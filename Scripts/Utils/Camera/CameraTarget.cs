using UnityEngine;

namespace Kuantech.Core.Utils
{
    public class CameraTarget : MonoBehaviour
    {
        public virtual Vector3 GetTargetPosition()
        {
            return transform.position;
        }
        
        public virtual Quaternion GetTargetRotation()
        {
            return transform.rotation;
        }
    }
}