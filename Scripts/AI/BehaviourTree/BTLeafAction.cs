using System;

namespace Kuantech.AI
{
    [Serializable]
    public class BTLeafAction
    {
        public virtual void EnterNode(BehaviourTree ownerTree){}
        public BTLeaf ParentNode;
        public virtual BTNode.NodeStatus Tick(BehaviourTree ownerTree)
        {
            return BTNode.NodeStatus.SUCCESS;
        }

        public virtual void ExitNode()
        {

        }
    }
}