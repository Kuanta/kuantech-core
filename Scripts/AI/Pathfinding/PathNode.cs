using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.AI.Pathfinding
{
    [Serializable]
    public class PathNode
    {
        public string Name = "Node";
        public float GCost = float.MaxValue;
        public float HCost;
        public float FCost => GCost + HCost;
        public bool Passable = true;
        [SerializeField] private float TraverseCost = 0f;

        [NonSerialized] public PathNodeComponent ParentNodeComponent;
        [NonSerialized] public Vector3 Position;
        [NonSerialized] public Quaternion Rotation;
        [NonSerialized] public PathNode ParentNode;
        [NonSerialized] public List<PathNode> ConnectedNodes;
        
        /// <summary>
        /// Returns the position of the node
        /// </summary>
        /// <returns></returns>
        public virtual Vector3 GetPosition()
        {
            if (ParentNodeComponent != null)
            {
                return ParentNodeComponent.transform.position;
            }
            return Position;
        }

        public virtual Quaternion GetRotation()
        {
            if (ParentNodeComponent != null) return ParentNodeComponent.transform.rotation;
            return Rotation;
        }
        public List<PathNode> GetConnectedNodes()
        {
            return ConnectedNodes;
        }

        public void ConnectToNode(PathNode pathNode)
        {
            if (ConnectedNodes == null)
            {
                ConnectedNodes = new List<PathNode>();
            }
            ConnectedNodes.Add(pathNode);
        }

        public bool IsPassable()
        {
            if (ParentNodeComponent != null)
            {
                return Passable && ParentNodeComponent.IsPassable();
            }
            return Passable;
        }
        public float GetTraverseCost()
        {
            float traverseCost = TraverseCost;
            if (ParentNodeComponent != null)
            {
                traverseCost += ParentNodeComponent.GetTraverseCost();
            }

            return traverseCost;
        }
    }
}