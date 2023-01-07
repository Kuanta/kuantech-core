using System;
using UnityEngine;

namespace Kuantech.Core.Utils
{
    [Serializable]
    public class QuadraticBezierCurve
    {
        public Vector3 P0;
        public Vector3 P1;
        public Vector3 P2;

        public QuadraticBezierCurve(Vector3 p0, Vector3 p1, Vector3 p2)
        {
            P0 = p0;
            P1 = p1;
            P2 = p2;
        }

        public Vector3 GetPoint(float t)
        {
            t = Mathf.Clamp(t, 0f, 1f);
            return (1 - t) * (1 - t) * P0 + 2 * (1 - t) * t * P1 + t * t * P2;
        }

        public static Vector3 GetPointFromTransforms(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            t = Mathf.Clamp(t, 0f, 1f);
            return (1 - t) * (1 - t) * p0 + 2 * (1 - t) * t * p1 + t * t * p2;
        }

        public static Vector3 GetTangentAtT(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            Vector3 current = GetPointFromTransforms(p0, p1, p2, t);
            Vector3 next;
            if (t >= 1)
            {
                next = GetPointFromTransforms(p0, p1, p2, t - 0.1f);
                return (current - next).normalized;
            }
            next = GetPointFromTransforms(p0, p1, p2, t + 0.1f);
            return (next - current).normalized;
        }
        
        public static Vector3 GetQuadraticBezierDerivative(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            t = Mathf.Clamp(t, 0f, 1f);
            return 2 * (1 - t) * (p1 - p0) + 2 * t * (p2 - p1);
        }
    }
}