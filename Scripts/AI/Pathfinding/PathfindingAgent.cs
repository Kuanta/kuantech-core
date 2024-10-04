using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.AI.Pathfinding
{
    public class PathfindingAgent : MonoBehaviour
    {
        [SerializeField] private WaypointFollower WaypointFollower;
        [SerializeField] private float WaypointFollowerSpeed;
        public PathNode CurrentNode;
        
        //Events
        public UnityAction OnReachedTargetEvent;
        public UnityAction OnReachedNodeEvent;

        public void Initialize()
        {
            if (WaypointFollower == null) return;
            WaypointFollower.OnReachedWaypoint += OnReachedWaypoint;
            WaypointFollower.OnReachedFinalTarget += OnReachedTarget;
        }
        
        public bool GoToNode(PathNode node, PathfinderNodeTree tree)
        {
            Path shortestPath = tree.FindPath(CurrentNode, node);
            if (shortestPath.PathNodes.IsNullOrEmpty())
            {
                Debug.LogError($"No Path to {node.Name}");
                return false;
            }
            SetPath(shortestPath.PathNodes.ToArray());
            FollowPath();
            return true;
        }
        
        public void SetPath(PathNode[] nodes)
        {
            List<WaypointFollower.Waypoint> waypoints = new List<WaypointFollower.Waypoint>();
            foreach (var node in nodes)
            {
                WaypointFollower.Waypoint wp = new WaypointFollower.Waypoint();
                wp.Position = node.GetPosition();
                wp.Rotation = node.GetRotation();
                wp.UserData = node;
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

        private void OnReachedWaypoint(WaypointFollower.Waypoint waypoint)
        {
            OnReachedNodeEvent?.Invoke();
        }
    }
}