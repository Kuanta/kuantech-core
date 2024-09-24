using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Utils.Math
{
    public class CatmullRomSpline
    {
        public static List<Vector3> ConstructSpline(List<Vector3> waypoints, int samplesPerSegment)
        {
            List<Vector3> splinePoints = new List<Vector3>();
            if (waypoints.Count < 2)
            {
                Debug.LogError("Not enough waypoints to form a path.");
                return waypoints;
            }
            // For each set of 4 waypoints (or clamped points for the ends)
            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                Vector3 p0 = (i == 0) ? waypoints[i] : waypoints[i - 1]; // Clamp to start
                Vector3 p1 = waypoints[i];
                Vector3 p2 = waypoints[Mathf.Min(i + 1, waypoints.Count - 1)];
                Vector3 p3 = waypoints[Mathf.Min(i + 2, waypoints.Count - 1)]; // Clamp to end

                // Generate spline points between p1 and p2
                for (int j = 0; j <= samplesPerSegment; j++)
                {
                    float t = j / (float)samplesPerSegment;
                    Vector3 point = GetCatmullRomPosition(t, p0, p1, p2, p3);
                    splinePoints.Add(point);
                }
            }

            return splinePoints;
        }
        
        public static Vector3 GetCatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            Vector3 result = 0.5f * (
                (2 * p1) +
                (-p0 + p2) * t +
                (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 +
                (-p0 + 3 * p1 - 3 * p2 + p3) * t3
            );
            return result;
        }
    }
}