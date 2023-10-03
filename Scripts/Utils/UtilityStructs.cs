
using UnityEngine;

namespace Kuantech.Utils
{
    public struct WorldPoint
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public Transform Target;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;

        public Vector3 GetTargetPosition()
        {
            return Target != null ? Target.TransformPoint(LocalPosition) : Position;
        }
    }
}