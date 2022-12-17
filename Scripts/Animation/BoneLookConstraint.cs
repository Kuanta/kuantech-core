using UnityEngine;

namespace Kuantech.Core.Animation
{
    public class BoneLookConstraint : MonoBehaviour
    {
        public Transform Bone;
        public Transform Target;
        public Vector3 RotationOffset = Vector3.zero;
        public Vector3 Offset = Vector3.zero;
        [Range(0f, 1f)]
        public float Weight;
        private void LateUpdate()
        {
            Quaternion lookRot = Quaternion.LookRotation((Target.position + Offset) - Bone.position);
            Bone.transform.rotation = Quaternion.Slerp(Bone.transform.rotation, lookRot * Quaternion.Euler(RotationOffset), Weight);
        }
    }
}