using System.Collections.Generic;
using Kuantech.Core.Utils;
using UnityEngine;
using UnityEngine.Serialization;

namespace Kuantech.Midcore.UI
{
    public static class BezierUtility
    {
        public static Vector3[] SampleBezier(List<Vector3> points, int resolution)
        {
            var result = new Vector3[resolution];
            for (int i = 0; i < resolution; i++)
            {
                float t = i / (float)(resolution - 1);
                result[i] = DeCasteljau(points, t);
            }
            return result;
        }

        public static Vector3 DeCasteljau(List<Vector3> points, float t)
        {
            var temp = new List<Vector3>(points);
            while (temp.Count > 1)
            {
                for (int i = 0; i < temp.Count - 1; i++)
                    temp[i] = Vector3.Lerp(temp[i], temp[i + 1], t);
                temp.RemoveAt(temp.Count - 1);
            }
            return temp[0];
        }
    }

    /// <summary>
    /// A UI component that draws a connector line between points using local positions (UI-friendly).
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class ConnectorUILine : MonoBehaviour
    {
        [FormerlySerializedAs("line")] [SerializeField] private LineRenderer LineRenderer;

        [Header("Anchors")]
        public RectTransform StartRect;
        public RectTransform EndRect;

        [Header("Optional Control Points")]
        public List<RectTransform> ControlPoints;

        [Header("Curve Settings")]
        public bool UseBezier = true;
        [Range(2, 64)] public int Resolution = 16;

        private void Awake()
        {
            if (LineRenderer == null)
                LineRenderer = GetComponent<LineRenderer>();

            LineRenderer.useWorldSpace = false; // UI-style line
        }

        private void Start()
        {
            if (StartRect == null || EndRect == null || LineRenderer == null) return;

            if (!UseBezier)
                DrawStraightLineWithControlPoints();
            else
                DrawBezierCurve();
        }

        private void DrawStraightLineWithControlPoints()
        {
            var points = new List<Vector3>
            {
                WorldToLocal(StartRect)
            };

            foreach (var cp in ControlPoints)
            {
                if (cp != null)
                    points.Add(WorldToLocal(cp));
            }

            points.Add(WorldToLocal(EndRect));

            LineRenderer.positionCount = points.Count;
            LineRenderer.SetPositions(points.ToArray());
        }

        private void DrawBezierCurve()
        {
            Vector3 p0 = WorldToLocal(StartRect);
            Vector3 p2 = WorldToLocal(EndRect);

            if (ControlPoints != null && ControlPoints.Count >= 1 && ControlPoints[0] != null)
            {
                var allPoints = new List<Vector3> { p0 };
                foreach (var cp in ControlPoints)
                    if (cp != null) allPoints.Add(WorldToLocal(cp));
                allPoints.Add(p2);

                var curvePoints = BezierUtility.SampleBezier(allPoints, Resolution);
                LineRenderer.positionCount = curvePoints.Length;
                LineRenderer.SetPositions(curvePoints);
            }
            else
            {
                Vector3 p1 = (p0 + p2) / 2 + Vector3.up * 100f;
                QuadraticBezierCurve curve = new QuadraticBezierCurve(p0, p1, p2);

                LineRenderer.positionCount = Resolution;
                for (int i = 0; i < Resolution; i++)
                {
                    float t = i / (float)(Resolution - 1);
                    LineRenderer.SetPosition(i, curve.GetPoint(t));
                }
            }
        }

        private Vector3 WorldToLocal(RectTransform rt)
        {
            return transform.InverseTransformPoint(rt.position);
        }
    }
}
