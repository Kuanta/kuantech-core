using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.AI.Utils
{
    public class WorldZone : MonoBehaviour {
        public List<Vector3> Points = new List<Vector3>();

        public WorldPoint SampleWorldPoint()
        {
            // Ensure there are at least 3 points to form a polygon
            if (Points.Count < 3)
            {
                Debug.LogError("Not enough points to form a polygon.");
                return new WorldPoint
                {
                    Target = transform,
                    LocalPosition = Vector3.zero,
                    LocalRotation = Quaternion.identity,
                };
            }

            // Pick a random triangle within the polygon
            int firstIndex = Random.Range(0, Points.Count);
            int secondIndex = (firstIndex + 1) % Points.Count;
            int thirdIndex = (firstIndex + 2) % Points.Count;

            Vector3 point1 = Points[firstIndex];
            Vector3 point2 = Points[secondIndex];
            Vector3 point3 = Points[thirdIndex];

            // Generate barycentric coordinates
            float alpha = Random.Range(0f, 1f);
            float beta = Random.Range(0f, 1f - alpha);
            float gamma = 1 - alpha - beta;

            // Calculate and return the random point inside the triangle
            Vector3 localPoint =  alpha * point1 + beta * point2 + gamma * point3;

            return new WorldPoint{
                Target = transform,
                LocalPosition = localPoint,
                LocalRotation = Quaternion.identity,
            };
        }
#if UNITY_EDITOR
         void OnDrawGizmos()
    {
        if (Points.Count > 1)
        {
            for (int i = 0; i < Points.Count - 1; i++)
            {
                Gizmos.DrawLine(transform.TransformPoint(Points[i]), transform.TransformPoint(Points[i + 1]));
            }
            // Connect the last point to the first
            Gizmos.DrawLine(transform.TransformPoint(Points[Points.Count - 1]), transform.TransformPoint(Points[0]));
        }
    }
#endif
    }
}