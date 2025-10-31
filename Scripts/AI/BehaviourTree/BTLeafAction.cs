using System;

namespace Kuantech.AI
{
    [Serializable]
    public class BTLeafAction
    {
        public virtual void EnterNode(BehaviourTree ownerTree){}
        public BTLeaf ParentNode;
        public bool ExecuteNextImmediately = false;
        
        public virtual BTNode.NodeStatus Tick(BehaviourTree ownerTree)
        {
            //Non overriden Ticks should return successs
            return BTNode.NodeStatus.SUCCESS;
        }

        public virtual void ExitNode()
        {

        }
    }
}