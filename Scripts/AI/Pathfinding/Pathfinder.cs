using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.AI.Pathfinding
{
    public struct Path
    {
        public List<PathNode> PathNodes;
        public float TotalCost;
    }
    /// <summary>
    /// Node based path finder using A* algorithm
    /// </summary>
    public class Pathfinder
    {
        public Path GetShortestPath(PathNode startNode, PathNode endNode)
        {
            List<PathNode> nodes = new List<PathNode>();
            List<PathNode> openList = new List<PathNode>();
            List<PathNode> closedList = new List<PathNode>();
            
            startNode.GCost = 0;
            startNode.HCost = CalculateHeuristicCost(startNode, endNode);
            openList.Add(startNode);

            while (openList.Count > 0)
            {
                PathNode currentNode = GetLowestFCostNode(openList);
                if (currentNode == null)
                {
                    Debug.LogError("What??");
                }
                if (currentNode == endNode)
                {
                    // Path found, return it
                    List<PathNode> pathNodes = RetracePath(startNode, endNode);
                    
                    return new Path()
                    {
                        PathNodes = pathNodes,
                        TotalCost = endNode.GCost,
                    };
                }
                
                // Move current node from open to closed list
                openList.Remove(currentNode);
                closedList.Add(currentNode);
                
                // Evaluate neighbors
                var connectedNodes = currentNode.GetConnectedNodes();
                if (connectedNodes == null)
                {
                    Debug.LogError($"Null at {currentNode.ParentNodeComponent.name}");
                    continue;
                }
                foreach (PathNode neighbor in currentNode.GetConnectedNodes())
                {
                    if(neighbor == null) continue;
                    if (closedList.Contains(neighbor))
                    {
                        continue; // Ignore already evaluated nodes
                    }

                    float newGCost = currentNode.GCost + CalculateHeuristicCost(currentNode, neighbor) + neighbor.TraverseCost;
                    if (newGCost < neighbor.GCost || !openList.Contains(neighbor))
                    {
                        neighbor.GCost = newGCost;
                        neighbor.HCost = CalculateHeuristicCost(neighbor, endNode);
                        neighbor.ParentNode = currentNode;

                        if (!openList.Contains(neighbor))
                        {
                            openList.Add(neighbor);
                        }
                    }
                }
            }

            return new Path()
            {
                PathNodes = null,
                TotalCost = 0,
            };
        }

        public float CalculateHeuristicCost(PathNode node, PathNode endNode)
        {
            return GetDistance(node, endNode);
        }
        private float GetDistance(PathNode nodeA, PathNode nodeB)
        {
            // Simple Euclidean distance
            return Vector3.Distance(nodeA.GetPosition(), nodeB.GetPosition());
        }
        public PathNode GetLowestFCostNode(List<PathNode> openList)
        {
            PathNode lowestFCostNode = openList[0];
            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].FCost < lowestFCostNode.FCost ||
                    (openList[i].FCost == lowestFCostNode.FCost && openList[i].HCost < lowestFCostNode.HCost))
                {
                    lowestFCostNode = openList[i];
                }
            }
            return lowestFCostNode;
        }
        
        // Function to retrace the path from end to start node
        private List<PathNode> RetracePath(PathNode startNode, PathNode endNode)
        {
            List<PathNode> path = new List<PathNode>();
            PathNode currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode);
                currentNode = currentNode.ParentNode;
            }

            path.Reverse(); // Reverse the list to get the path from start to end
            return path;
        }
    }
}