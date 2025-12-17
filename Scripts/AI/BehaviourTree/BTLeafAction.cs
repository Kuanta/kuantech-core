using System;

namespace Kuantech.AI
{
    [Serializable]
    public class BTLeafAction
    {
        public virtual void Initialize(BehaviourTree ownerTree){}
        public virtual void EnterNode(BehaviourTree ownerTree){}
        public BTLeaf ParentNode;
        public bool ExecuteNextImmediately = false;
        [NonSerialized] protected BTNode.NodeStatus CurrentStatus = BTNode.NodeStatus.SUCCESS;
        
        public virtual BTNode.NodeStatus Tick(BehaviourTree ownerTree)
        {
            return CurrentStatus;
        }

        public virtual void ExitNode()
        {

        }
    }
}