using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.AI
{
    [Serializable]
    public class LeafActionVariablesDict : SerializableDictionary<string, string>
    {
        
    }
    [CreateAssetMenu(menuName = "AI/Behavior Tree Graph Data")]
    public class BehaviourTreeBlueprint : ScriptableObject
    {
        public BtGraphNodeData RootNodeData;

        #region BehaviourTree Generation
        public BehaviourTree CreateBehaviourTree()
        {
            BehaviourTree tree = new BehaviourTree();
            foreach (var childData in RootNodeData.ChildNodeData)
            {
                BTNode child = ProcessNode(childData);
                tree.AddChild(child);
            }
            return tree;
        }

        private static BTNode ProcessNode(BtGraphNodeData nodeData)
        {
            BTNode node = null;
            switch (nodeData.NodeType)
            {
                case BTNode.NodeTypes.SELECTOR:
                    node = new BTSelector(nodeData.NodeName);
                    break;
                case BTNode.NodeTypes.SEQUENCE:
                    node = new BTSequence(nodeData.NodeName);
                    break;
                case BTNode.NodeTypes.LEAF:
                    BTLeaf leafNode = new BTLeaf(nodeData.NodeName);
                    leafNode.ParseNodeData(nodeData);
                    return leafNode;
            }

            if (node == null)
            {
                throw new Exception("'How could this happen' - Heavy");
            }
            
            foreach (var childData in nodeData.ChildNodeData)
            {
                BTNode child = ProcessNode(childData);
                node.AddChild(child);
            }

            return node;
        }
        #endregion
    }
    
    [Serializable]
    public class BtGraphNodeData
    {
        //General  structure
        public string NodeName;
        public AI.BTNode.NodeTypes NodeType;
        public Vector2 NodePosition;
        public Vector2 NodeSize;
        public List<BtGraphNodeData> ChildNodeData;
        
        //Leaf Nodes
        public string ActionClassName;
        public LeafActionVariablesDict ActionClassVariables;
    }

}