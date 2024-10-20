using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.AI.Pathfinding
{
    public class PathNodeComponent : MonoBehaviour
    {
        private PathNode PathNode = null;
        public float AutoConnectRange = 5.0f;
        public float ConnectAngleRange = 360.0f;
        public List<PathNodeComponent> MaskedNodes;
        public List<PathNodeComponent> MustConnectNodes;
        public bool RemoveFromAutoConnect;
        public List<GameObject> ConnectedNodesGameObjects;
        
        public void Initialize()
        {
            CreatePathNode();
            if (ConnectedNodesGameObjects != null)
            {
                PathNode.ConnectedNodes = new List<PathNode>();
                foreach (var connectedObj in ConnectedNodesGameObjects)
                {
                    if (connectedObj == null)
                    {
                        Debug.LogError($"Null connection in {name}");
                        continue;
                    }
                    if (connectedObj == gameObject) continue;
                    if (connectedObj.TryGetComponent(out PathNodeComponent pathNodeComponent))
                    {
                        PathNode.ConnectedNodes.Add(pathNodeComponent.PathNode);
                    }
                }
            }
        }

        public void CreatePathNode()
        {
            if (PathNode != null) return;
            PathNode ??= new PathNode();
            PathNode.ParentNodeComponent = this;
            PathNode.Position = transform.position;
            PathNode.Rotation = transform.rotation;
            PathNode.ConnectedNodes = new List<PathNode>();
        }
        
        public bool CanConnectToNode(PathNodeComponent otherNode)
        {
            if (MaskedNodes.Contains(otherNode)) return false;
            if (MustConnectNodes.Contains(otherNode))
            {
                return true;
            }
            //Check direction condition
            Vector3 diff = otherNode.transform.position - transform.position;
            Vector3 projected = Vector3.ProjectOnPlane(diff, transform.up);
            float angle = Vector3.SignedAngle(transform.forward, projected, transform.up);
            if (Mathf.Abs(angle) > ConnectAngleRange * 0.5f)
            {
                return false;
            }
            //Check distance condition
            float dist = (transform.position - otherNode.transform.position).sqrMagnitude;
            if (dist <= AutoConnectRange * AutoConnectRange) return true;
            return false;
        }

        public virtual float GetTraverseCost()
        {
            return 0;
        }
        public virtual bool IsPassable()
        {
            return true;
        }
        
        public void ConnectToNode(PathNode node)
        {
            GetPathNode().ConnectedNodes.Add(node);
        }

        public void ConnectToNode(PathNodeComponent nodeComponent)
        {
            if (nodeComponent.PathNode == null)
            {
                nodeComponent.CreatePathNode();
            }

            if (PathNode == null)
            {
                CreatePathNode();
                
            }

            if (ConnectedNodesGameObjects == null) ConnectedNodesGameObjects = new List<GameObject>();
            if (!ConnectedNodesGameObjects.Contains(nodeComponent.gameObject))
            {
                ConnectedNodesGameObjects.Add(nodeComponent.gameObject);
            }
            if (IsConnectedToNode(nodeComponent)) return;
            ConnectToNode(nodeComponent.GetPathNode());
        }
        public bool IsConnectedToNode(PathNode node)
        {
            if (PathNode == null || PathNode.ConnectedNodes == null)
            {
                Debug.LogError($"Check {name}");
            }
            return PathNode.ConnectedNodes.Contains(node);
        }

        public bool IsConnectedToNode(PathNodeComponent nodeComponent)
        {
            if (nodeComponent == null || nodeComponent.PathNode == null) return false;
            return IsConnectedToNode(nodeComponent.PathNode);
        }

        public PathNode GetPathNode()
        {
            CreatePathNode();
            return PathNode;
        }
        #region Editor
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Set the color for the gizmo
            Gizmos.color = Color.red;

            // Draw a wireframe sphere at the object's position with the specified range
            Gizmos.DrawWireSphere(transform.position, AutoConnectRange);

            if (PathNode == null || PathNode.ConnectedNodes == null) return;
            foreach (var connected in PathNode.ConnectedNodes)
            {
                Ray r = new Ray()
                {
                    direction = connected.GetPosition() - transform.position,
                    origin = transform.position,
                };
                Gizmos.DrawRay(r);
            }
        }
#endif
        #endregion
    }
}