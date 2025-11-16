using UnityEngine;

namespace Kuantech.Core.Utils
{
    public class OrbitCameraTarget : CameraTarget
    {
        public Transform Anchor;
        public Vector3 AnchorOffset = Vector3.zero;
        public float Radius;
        public float PitchAngle;
        public float YawAngle;
        
        public override Vector3 GetTargetPosition()
        {
            Transform anchor = GetAnchor();
            if (anchor == null)
            {
                return Vector3.zero;
            }
            Vector3 offset = new Vector3
            {
                x = Radius * Mathf.Cos(Mathf.Deg2Rad * PitchAngle) * Mathf.Sin(Mathf.Deg2Rad * YawAngle),
                y = Radius * Mathf.Sin(Mathf.Deg2Rad * PitchAngle),
                z = Radius * Mathf.Cos(Mathf.Deg2Rad * PitchAngle) * Mathf.Cos(Mathf.Deg2Rad * YawAngle)
            };
            return anchor.position + offset + AnchorOffset;
        }
        
        public override Quaternion GetTargetRotation()
        {
            Transform anchor = GetAnchor();
            if (anchor == null)
            {
                return Quaternion.identity;
            }

            Vector3 targetPos = GetTargetPosition();
            return Quaternion.LookRotation(anchor.position + AnchorOffset - targetPos);
        }
    
        public Transform GetAnchor()
        {
            if (Anchor == null) return transform;
            return Anchor;
        }
    }
}