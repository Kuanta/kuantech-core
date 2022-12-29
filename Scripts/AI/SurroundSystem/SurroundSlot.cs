using System;
using UnityEngine;

namespace Kuantech.SurroundSystem
{
    [Serializable]
    public class SurroundSlot
    {
        public int Row = 0;
        public float Column = 0; //Column can be floating point like 0.5
        public float VerticalDistance = 1f;
        public float HorizontalDistance = 0f;
        
        public float LeftOffset = 2f;
        public float RightOffset = 0f;
        
        public Vector3 GetTargetPoint(Transform target)
        {
            Vector3 targetPoint;
            Vector3 forwardVector = target.forward * VerticalDistance * (Row+1);
            Vector3 rightVector = target.right * HorizontalDistance * Column;
            targetPoint = target.position + forwardVector + rightVector;
            targetPoint +=  target.right * LeftOffset -  target.right * RightOffset;
            return targetPoint;
        }
        
        /// <summary>
        /// Checks if agent has reached the target (or is near enough)
        /// </summary>
        /// <param name="target"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        public bool ReachedTarget(Transform target, Vector3 currentPosition, float threshold = 0.001f)
        {
            Vector3 diff = target.position - currentPosition;
            Vector3 forwardProjection = Vector3.Dot(-diff, target.forward) * target.forward;
            float targetVerticalDistance = GetVerticalDistance();
            return forwardProjection.sqrMagnitude <= targetVerticalDistance * targetVerticalDistance + threshold;
        }

        public float GetVerticalDistance()
        {
            return (Row + 1) * VerticalDistance;
        }

        public float GetHorizontalDistance()
        {
            return Column * HorizontalDistance;
        }
    }
}