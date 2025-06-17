using Kuantech.Core;
using Pathfinding;
using UnityEngine;

namespace Kuantech.ThirdParty.AStarPathfindingProject
{
    /// <summary>
    /// This module is used to add the A* Pathfinding Project to the game.
    /// </summary>
    public class APPActorModule : ActorModule
    {
        public FollowerEntity AIAgent;

        private void Update()
        {
            if (Actor == null || !Actor.IsAlive()) return;
            
            //Go to clicked point
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                worldPoint.z = 0;
                AIAgent.destination = worldPoint;
                AIAgent.SearchPath();
            }
        }
    }
}