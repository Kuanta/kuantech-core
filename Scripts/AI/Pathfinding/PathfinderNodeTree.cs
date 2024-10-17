using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.AI.Pathfinding
{
    public class PathfinderNodeTree : MonoBehaviour
    {
        public List<PathNode> Nodes;
        public List<PathNodeComponent> NodeComponents;
   
        public void Initialize()
        {
            foreach (var nodeComp in NodeComponents)
            {
                nodeComp.Initialize();
                Nodes.Add(nodeComp.PathNode);
            }
        }

        /// <summary>
        /// Returns the closest node
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public PathNode GetClosestNode(Vector3 position)
        {
            PathNode closestNode = null;
            float shortestDistance = float.MaxValue;
            foreach (var node in Nodes)
            {
                float sqrDist = Vector3.SqrMagnitude(node.GetPosition() - position);
                if (sqrDist < shortestDistance)
                {
                    shortestDistance = sqrDist;
                    closestNode = node;
                }
            }

            return closestNode;
        }

        public void DetectChildNodes()
        {
            NodeComponents = GetComponentsInChildren<PathNodeComponent>().ToList();
        }
        [Button("Connect Nodes")]
        public void AutoConnectNodes()
        {
            DetectChildNodes();
            foreach (var nodeComp in NodeComponents)
            {
                nodeComp.PathNode.ConnectedNodes = new List<PathNode>();
                nodeComp.ConnectedNodesGameObjects = new List<GameObject>();
                nodeComp.PathNode.ParentNodeComponent = nodeComp;
                
                //Check must connect nodes
                foreach (var mustConnect in nodeComp.MustConnectNodes)
                {
                    nodeComp.ConnectToNode(mustConnect);
                }
                if(nodeComp.RemoveFromAutoConnect) continue;
                foreach (var other in NodeComponents)
                {
                    if(nodeComp == other || other.RemoveFromAutoConnect) continue;
                    if(!nodeComp.CanConnectToNode(other)) continue;
                    nodeComp.ConnectToNode(other);
                }
            }
        }
    }
}