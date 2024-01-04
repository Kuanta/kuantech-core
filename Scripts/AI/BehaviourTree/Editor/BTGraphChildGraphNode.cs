using Kuantech.AI;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Search;

namespace Kuantech.Editor
{
    public class BTSubTreeNode : BtGraphNode
    {
        public BehaviourTreeBlueprint ChildGraph;
        private ObjectField _childGraphField;
        public BTSubTreeNode(GraphView parentGraphView) : base(parentGraphView)
        {
            _childGraphField = EditorUtilities.CreateFieldForType(typeof(BehaviourTreeBlueprint), "SubTree") as ObjectField;
            mainContainer.Add(_childGraphField);
            NodeType = BTNode.NodeTypes.SUB_GRAPH;
        }

        public override void LoadSaveData(BtGraphNodeData data)
        {
            base.LoadSaveData(data);
            ChildGraph = data.SubTree;
           _childGraphField.value = ChildGraph;
        }

        public override BtGraphNodeData GetSaveData()
        {
            BtGraphNodeData data = base.GetSaveData();
            data.SubTree = _childGraphField.value as BehaviourTreeBlueprint;
            return data;
        }
    }
}