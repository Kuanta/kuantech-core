using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.AI.Pathfinding
{
    public class PathNodeComponent : MonoBehaviour
    {
        public PathNode PathNode;
        public float AutoConnectRange = 5.0f;
        public float ConnectAngleRange = 360.0f;
        public List<PathNodeComponent> MaskedNodes;
        public List<PathNodeComponent> MustConnectNodes;
        public bool RemoveFromAutoConnect;
        public void Initialize()
        {
            PathNode ??= new PathNode();
            PathNode.ParentNodeComponent = this;
            PathNode.Position = transform.position;
            PathNode.Rotation = transform.rotation;
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

        public void ConnectToNode(PathNode node)
        {
            PathNode.ConnectedNodes.Add(node);
        }

        public void ConnectToNode(PathNodeComponent nodeComponent)
        {
            if (nodeComponent == null || nodeComponent.PathNode == null)
            {
                Debug.LogError("What?");
                return;
            }
            if (IsConnectedToNode(nodeComponent)) return;
            ConnectToNode(nodeComponent.PathNode);
        }
        public bool IsConnectedToNode(PathNode node)
        {
            return PathNode.ConnectedNodes.Contains(node);
        }

        public bool IsConnectedToNode(PathNodeComponent nodeComponent)
        {
            if (nodeComponent == null || nodeComponent.PathNode == null) return false;
            return IsConnectedToNode(nodeComponent.PathNode);
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