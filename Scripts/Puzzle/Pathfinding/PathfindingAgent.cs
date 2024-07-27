using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Puzzle.Pathfinding
{
    public class PathfindingAgent : MonoBehaviour
    {
        [SerializeField] private WaypointFollower WaypointFollower;
        [SerializeField] private float WaypointFollowerSpeed;
        
        //Events
        public UnityAction OnReachedTargetEvent;
        
        public void SetPath(PathNode[] nodes)
        {
            WaypointFollower.ReachedFinalTarget = OnReachedTarget;
            List<WaypointFollower.Waypoint> waypoints = new List<WaypointFollower.Waypoint>();
            foreach (var node in nodes)
            {
                WaypointFollower.Waypoint wp = new WaypointFollower.Waypoint();
                wp.Position = node.GetPosition();
                wp.Speed = WaypointFollowerSpeed;
                waypoints.Add(wp);
            }
            WaypointFollower.SetWaypoints(waypoints);
        }

        public void FollowPath()
        {
            WaypointFollower.FollowPath();
        }

        private void OnReachedTarget()
        {
            OnReachedTargetEvent?.Invoke();
        }
    }
}