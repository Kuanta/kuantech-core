using System;
using System.Collections.Generic;
using Kuantech.ArcadeIdle;
using Kuantech.Utils.Math;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.Utils
{
    public class SplineWaitingQueue : WaitingQueue
    {
        [Header("Spline Parameters")] public Transform WaypointsParent;
        [NonSerialized] public List<Transform> Waypoints;
        public int SplineDegree = 5;
        public int SplineResolution = 10;
        public int RotationLookAhead = 5;
        public float RegularSpeed = 8;
        public float InitialSpeed = 20;
        public float InitialSpeedDecay = 0.9f;
        public float InitialDistanceOffset = 0;
        private BSpline _spline;
        public bool MoveToInitialPoints;
        public override void Initialize()
        {
            _spline = new BSpline
            {
                InvertDirection = true
            };
            Waypoints = new List<Transform>();
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

        [Button("Mirror Waypoints")]
        public void MirrorWaypoints()
        {
            for (int i = 0; i < WaypointsParent.childCount; ++i)
            {
                Transform childTransform = WaypointsParent.GetChild(i);
                childTransform.position = new Vector3(childTransform.position.x * -1, childTransform.position.y,
                    childTransform.position.z);
            }
        }

        public float GetTotalSplineDistance()
        {
            return _spline.GetTotalDistance();
        }
        
        public override void QueueElement(IWaitingQueueElement element, bool updatePositions=true)
        {
            if (IsQueueFull()) return;
            element.GetSplineFollower().CurrentSpline = _spline;
            base.QueueElement(element,updatePositions);
        }
        
        [Button("Update Positions")]
        public override void UpdateQueuePositions(bool warpToPosition)
        {
            totalDistance = _spline.GetTotalDistance();
            if (WaitingElements.IsNullOrEmpty()) return;
            float initialDistance = WaitingElements.Peek().GetSize() * WaitingElements.Count + InitialDistanceOffset;
            float initialSpeed = InitialSpeed;
            foreach (var actor in WaitingElements)
            {
                actor.SetSpline(_spline);
                var splineFollower = actor.GetSplineFollower();
                
                //todo: Fix this mess
                if (!MoveToInitialPoints)
                {
                    if (!warpToPosition)
                    {
                        splineFollower.FollowSpeed = RegularSpeed;
                        splineFollower.GoToDistance(totalDistance);
                    }
                    else
                    {
                        splineFollower.SetPositionWithDistance(totalDistance);
                        // splineFollower.SetPositionWithDistance(initialDistance);
                        // splineFollower.FollowSpeed = initialSpeed;
                        // initialSpeed *= InitialSpeedDecay;
                    }
                }
                else
                {
                    splineFollower.SetCurrentDistance(totalDistance);
                    actor.GoToPosition(_spline.GetPointAtDistance(totalDistance));
                }
                totalDistance -= actor.GetSize();
            }
            
            // totalDistance = 0f;
            // if (WaitingElements.IsNullOrEmpty()) return;
            // float initialDistance = WaitingElements.Peek().GetSize() * WaitingElements.Count + InitialDistanceOffset;
            // float initialSpeed = InitialSpeed;
            // foreach (var actor in WaitingElements)
            // {
            //     actor.SetSpline(_spline);
            //     var splineFollower = actor.GetSplineFollower();
            //     
            //     //todo: Fix this mess
            //     if (!MoveToInitialPoints)
            //     {
            //         if (!warpToPosition)
            //         {
            //             splineFollower.FollowSpeed = RegularSpeed;
            //             splineFollower.GoToDistance(totalDistance);
            //         }
            //         else
            //         {
            //             splineFollower.SetPositionWithDistance(totalDistance);
            //             // splineFollower.SetPositionWithDistance(initialDistance);
            //             // splineFollower.FollowSpeed = initialSpeed;
            //             // initialSpeed *= InitialSpeedDecay;
            //         }
            //     }
            //     else
            //     {
            //         splineFollower.SetCurrentDistance(totalDistance);
            //         actor.GoToPosition(_spline.GetPointAtDistance(totalDistance));
            //     }
            //     totalDistance += actor.GetSize();
            // }
        }

        
#if UNITY_EDITOR   
        [Button("Create Spline")]
        public void CreateSplinePoints()
        {
            _spline = new BSpline();
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
#endif
        
    }
}