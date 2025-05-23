using System.Collections.Generic;
using System.Linq;
using Kuantech.AI.Pathfinding;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.TowerDefense
{
    /// <summary>
    /// Defines a path for the tower defense game.
    /// </summary>
    public class TowerDefensePath : MonoBehaviour
    {
        public PathNodeComponent StartNode;
        public PathNodeComponent EndNode;
        
        public List<PathNodeComponent> PathNodes = new List<PathNodeComponent>();

        public void Initialize()
        {
            PathNodes = GetComponentsInChildren<PathNodeComponent>().ToList();
            foreach (var nodeComponent in PathNodes)
            {
                nodeComponent.Initialize();
            }
        }
        
        public void SetActorOnPath(Actor actor)
        {
            TowerDefenseActorModule tdModule = actor.GetModule<TowerDefenseActorModule>();
            if (tdModule == null) return;
            Path path = Pathfinder.GetShortestPath(StartNode.GetPathNode(), EndNode.GetPathNode());
            if (!path.IsValidPath()) return;
            tdModule.SetOnPath(path);
        }
    }
}