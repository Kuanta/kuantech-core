
using System;
using UnityEngine;

namespace Kuantech.Utils
{
    [Serializable]
    public class WorldPoint
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public bool IsScreenPosition = false;
        public Transform Target;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        
        public Vector3 GetTargetPosition()
        {
            return Target != null ? Target.TransformPoint(LocalPosition) : Position;
        }

        public Quaternion GetRotation()
        {
            return Target != null ? Target.rotation * LocalRotation : Rotation;
        }

        public void PositionGameObject(GameObject gameObject)
        {
            if (Target != null)
            {
                gameObject.transform.SetParent(Target);
            }

            gameObject.transform.position = GetTargetPosition();
            gameObject.transform.rotation = GetRotation();
        }
    }
}