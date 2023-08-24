using System;

namespace Kuantech.AI
{
    [Serializable]
    public class BTLeafAction
    {
        public virtual void EnterNode(BehaviourTree ownerTree){}

        public virtual BTNode.NodeStatus Tick(BehaviourTree ownerTree)
        {
            return BTNode.NodeStatus.SUCCESS;
        }
    }
}