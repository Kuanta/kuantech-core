using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.AI.Pathfinding
{
    public class PathfindingAgent : MonoBehaviour
    {
        [SerializeField] private WaypointFollower WaypointFollower;
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

        public bool IsMoving()
        {
            return WaypointFollower.IsMoving();
        }

        public Path GetShortestPath(PathNode node)
        {
            return Pathfinder.GetShortestPath(CurrentNode, node);
        }

        public Path GetShortestPath(PathNode startNode, PathNode endNode)
        {
            return Pathfinder.GetShortestPath(startNode, endNode);
        }

        public void GoToWorldPoint(WorldPoint point)
        {
            WaypointFollower.GoToWorldPoint(point);
        }
        public bool GoToNode(PathNode node)
        {
            Path shortestPath = GetShortestPath(node);
            if (!shortestPath.IsValidPath())
            {
                return false;
            }
            SetPath(shortestPath);
            FollowPath();
            return true;
        }

        public void SetPath(Path path)
        {
            SetPath(path.PathNodes.ToArray());
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

        public void Stop()
        {
            WaypointFollower.Stop();
        }
    }
}