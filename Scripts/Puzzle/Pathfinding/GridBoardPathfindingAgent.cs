using System.Collections.Generic;
using Kuantech.AI.Pathfinding;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Puzzle.Pathfinding
{
    public class GridBoardPathfindingAgent : MonoBehaviour
    {
        [SerializeField] private WaypointFollower WaypointFollower;
        
        //Events
        public UnityAction OnReachedTargetEvent;
        
        public void SetPath(PathNode[] nodes)
        {
            WaypointFollower.OnReachedFinalTarget = OnReachedTarget;
            List<WaypointFollower.Waypoint> waypoints = new List<WaypointFollower.Waypoint>();
            foreach (var node in nodes)
            {
                WaypointFollower.Waypoint wp = new WaypointFollower.Waypoint();
                wp.Position = node.GetPosition();
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