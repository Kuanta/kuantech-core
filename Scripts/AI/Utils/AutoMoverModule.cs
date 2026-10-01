using System.Collections.Generic;
using Kuantech.AI.Pathfinding;
using Kuantech.Utils;
using Kuantech.Utils.Math;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Core
{
    public class AutoMoverModule : ActorModule
    {
        [Header("Components")] 
        [SerializeField] private WaypointFollower WaypointFollower;
        [SerializeField] private SplineFollower SplineFollower;
        [SerializeField] private PathFollower PathFollower;

        [Header("Properties")] 
        [SerializeField] private float Speed;
        
        //States
        private bool _isMoving;
        
        //Events
        public UnityAction OnReachedTargetEvent;

        public override void Initialize()
        {
            base.Initialize();
            if(WaypointFollower != null) WaypointFollower.OnReachedFinalTarget += OnReachedTarget;
            if(SplineFollower != null) SplineFollower.OnReachedTarget += OnReachedTarget;
            if(PathFollower != null) PathFollower.OnReachedPathEnd += OnReachedTarget;
        }
        
        #region Movement Controls
        /// <summary>
        /// Follows a path
        /// </summary>
        /// <param name="path"></param>
        public void FollowPath(Path path)
        {
            if (PathFollower == null) return;
            StartMoving();
        }

        public void FollowSpline(BSpline spline)
        {
            if (SplineFollower == null) return;
            SplineFollower.FollowSpline(spline);
            StartMoving();
        }
        
        /// <summary>
        /// Starts following a spline created from given control points
        /// </summary>
        /// <param name="controlPoints"></param>
        public void FollowSplinePoints(List<Vector3> controlPoints)
        {
            if (SplineFollower == null) return;
            SplineFollower.CreateSplineFromControlPoints(controlPoints);
            SplineFollower.FollowSpline();
            StartMoving();
        }
        
        /// <summary>
        /// Goes to a waypoint
        /// </summary>
        public void GoToWaypoint(Vector3 position)
        {
            if (WaypointFollower == null) return;
            WaypointFollower.GoToWorldPoint(new WorldPoint()
            {
                Position = position,
            });
            WaypointFollower.Follow();
            StartMoving();
        }
        
        /// <summary>
        /// Starts following wypoints
        /// </summary>
        /// <param name="waypoints"></param>
        public void FollowWaypoints(List<Vector3> waypoints)
        {
            if (WaypointFollower == null) return;
            WaypointFollower.SetWaypoints(waypoints);
            WaypointFollower.Follow();
            StartMoving();
            
        }
        
        /// <summary>
        /// Called when moving starts
        /// </summary>
        private void StartMoving()
        {
            _isMoving = true;
        }
        #endregion

        #region Event handlers

        private void OnReachedTarget()
        {
            _isMoving = false;
            OnReachedTargetEvent?.Invoke();
        }

        #endregion
    }
}