using System;
using System.Collections.Generic;
using Kuantech.Utils.Math;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.Utils
{
    public class SplineEditorTools : MonoBehaviour
    {
        [Header("Spline Parameters")] public Transform WaypointsParent;
        [NonSerialized] public List<Transform> Waypoints;
        public int SplineDegree = 5;
        public int SplineResolution = 10;
        public int RotationLookAhead = 5;
        public bool LoopingSpline;
        private BSpline _spline;
#if UNITY_EDITOR   
        [Button("Create Spline")]
        public void CreateSplinePoints()
        {
            _spline = new BSpline();
            _spline.Looping = LoopingSpline;
            _spline.InvertDirection = true;
            Waypoints = new List<Transform>();
            if (WaypointsParent == null) return;
            for (int i = 0; i < WaypointsParent.childCount; ++i)
            {
                Waypoints.Add(WaypointsParent.GetChild(i));
            }
            List<Vector3> controlPoints = new List<Vector3>();
            foreach (var controlPoint in Waypoints)
            {
                controlPoints.Add(controlPoint.position);
            }
            _spline.SetSplinePoints(controlPoints, SplineDegree, SplineResolution);
            _spline.RotationLookAhead = RotationLookAhead;
        }

        private void OnDrawGizmos()
        {
            CreateSplinePoints();
            // En az iki nokta yoksa çizim yapılmaz
            if (_spline.SplinePoints == null || _spline.SplinePoints.Count < 2) return;

            Gizmos.color = Color.red;  // Çizilecek çizginin rengi

            // SplinePoints içindeki noktaları birbirine bağlayan çizgiler çiz
            for (int i = 0; i < _spline.SplinePoints.Count - 1; i++)
            {
                Vector3 startPoint = _spline.SplinePoints[i] - transform.position;
                Vector3 endPoint = _spline.SplinePoints[i + 1] - transform.position;

                // World space'e dönüştürmek için transform pozisyonunu ekle
                Gizmos.DrawLine(transform.position + startPoint, transform.position + endPoint);
            }
        }

        [Button("Mirror Points")]
        public void MirrorHorizontally()
        {
            for (int i = 0; i < WaypointsParent.childCount; ++i)
            {
                Transform child = WaypointsParent.GetChild(i);
                Vector3 oldLocal = child.transform.localPosition;
                oldLocal.x *= -1;
                child.transform.localPosition = oldLocal;
            }
        }
#endif
    }
}