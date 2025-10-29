
using System;
using UnityEngine;

namespace Kuantech.Utils
{
    [Serializable]
    public class WorldPoint
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public float OrthographicSize;
        public bool IsScreenPosition = false;
        public Transform Target;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        public Vector3 OffsetPosition;
        public float Radius; //Used for actor radius, or to represent a circular point
        
        public Vector3 GetTargetPosition()
        {
            Vector3 pos = Target != null ? Target.TransformPoint(LocalPosition) : Position;
            return pos + OffsetPosition;
        }
        
        /// <summary>
        /// Returns target position at a distance of Radius from the target position towards the given position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Vector3 GetTargetPositionTowardsTarget(Vector3 position)
        {
            Vector3 target = GetTargetPosition();
            Vector3 diff = position - target;
            diff.Normalize();
            return target + diff * Radius;
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