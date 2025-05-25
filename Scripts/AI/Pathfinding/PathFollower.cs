using System;
using System.Collections.Generic;
using Kuantech.Utils;
using Kuantech.Utils.Math;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.AI.Pathfinding
{
    public class PathFollower : MonoBehaviour
    {
        public enum FollowMethod
        {
            WaypointFollower,
            SplineFollower,
        }

        [Header("Follow Method")] 
        public FollowMethod CurrentFollowMethod;

        [Tooltip("If set to true, follower will teleport to spline start")] 
        public bool WarpToSplineStartOnFollow;
        
        [Header("Speed")] 
        [SerializeField] private float Speed = 5;
        
        [Header("Spline Follower")] 
        [SerializeField] private SplineFollower SplineFollower;
        [SerializeField] private int PathDegree = 3;
        [SerializeField] private int PathResolution = 10;
        
        [Header("Waypoint Follower")]
        [SerializeField] private WaypointFollower WaypointFollower;
        
        [NonSerialized] public Path CurrentPath;

        private bool _gointToPathStart;
        private bool _reachedPathStart; //First, waypoint follower goes to the path start
        
        //Events
        public UnityAction OnReachedPathEnd;

        public float GetSpeed()
        {
            return Speed;
        }
        
        public void SetFollowSpeed(float speed)
        {
            Speed = speed;
            if(SplineFollower != null) SplineFollower.SetSpeed(speed);
            if(WaypointFollower != null) WaypointFollower.SetSpeed(speed);
        }
        
        /// <summary>
        /// Sets the path
        /// </summary>
        /// <param name="path"></param>
        public void SetPath(Path path)
        {
            CurrentPath = path;
            if (SplineFollower != null)
            {
                path.UpdatePathSpline(PathResolution, PathDegree);
                SplineFollower.SetSpline(path.PathSpline);
                SplineFollower.OnReachedTarget -= OnReachedTarget;
                SplineFollower.OnReachedTarget += OnReachedTarget;
            }

            if (WaypointFollower != null)
            {
                List<WaypointFollower.Waypoint> waypoints = new List<WaypointFollower.Waypoint>();
                foreach (var node in path.PathNodes)
                {
                    waypoints.Add(new WaypointFollower.Waypoint()
                    {
                        Position = node.GetPosition(),
                    });
                }
                WaypointFollower.SetWaypoints(waypoints);
                WaypointFollower.OnReachedFinalTarget -= OnReachedTarget;
                WaypointFollower.OnReachedFinalTarget += OnReachedTarget;
            }
        }

        /// <summary>
        /// Follows the path
        /// </summary>
        /// <param name="path"></param>
        /// <param name="lateralOffset">Deviation offset</param>
        public void FollowPath(Path path, float lateralOffset = 0)
        {
            SetFollowSpeed(Speed);

            //First, check if need a waypoint
            SetPath(path);
            
            if (CurrentFollowMethod == FollowMethod.SplineFollower && SplineFollower != null)
            {
                SplineFollower.LateralOffset = lateralOffset;
                if (WarpToSplineStartOnFollow)
                {
                    SplineFollower.FollowSpline(path.PathSpline);
                }
                else
                {
                    GoToPathStart(path.PathSpline.GetPointAtT(0, lateralOffset).Position);
                }
            }

            if (CurrentFollowMethod== FollowMethod.WaypointFollower && WaypointFollower != null)
            {
                WaypointFollower.Follow();
            }
        }

        private void GoToPathStart(Vector3 start)
        {
            _gointToPathStart = true;
            if (WaypointFollower == null)
            {
                OnReachedTarget();
                return;
            }
            WaypointFollower.GoToWorldPoint(new WorldPoint()
            {
                Position = start,
            });
        }
        
        //Is moving
        public bool IsMoving()
        {
            bool splineFollowerMoving = false;
            if (SplineFollower != null)
            {
                splineFollowerMoving = SplineFollower.IsMoving();
            }

            bool waypointFollowerMoving = false;
            if (WaypointFollower != null)
            {
                waypointFollowerMoving = WaypointFollower.IsMoving();
            }

            return splineFollowerMoving || waypointFollowerMoving;
        }
        public void Stop()
        {
            if(SplineFollower != null) SplineFollower.Stop();
            if(WaypointFollower != null) WaypointFollower.Stop();
        }

        public void Pause()
        {
            
        }
        /// <summary>
        /// Returns movement vector
        /// </summary>
        /// <returns></returns>
        public Vector2 GetMovementVector()
        {
            return Vector2.up;
        }
        
        #region Event Handlers
        private void OnReachedTarget()
        {
            if (_gointToPathStart)
            {
                if (CurrentFollowMethod != FollowMethod.SplineFollower || SplineFollower == null)
                {
                    Debug.LogError("Follow method is incorrect in path follower");
                    return;
                }
                _gointToPathStart = false;
                SplineFollower.FollowSpline(CurrentPath.PathSpline);
                return;
            }
            OnReachedPathEnd?.Invoke();
        }
        
        #endregion
    }
}