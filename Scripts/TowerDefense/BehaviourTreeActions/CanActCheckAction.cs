using Kuantech.AI;
using Kuantech.Core;

namespace Kuantech.TowerDefense
{
    public class CanActCheckAction : BTLeafAction
    {
        public override BTNode.NodeStatus Tick(BehaviourTree ownerTree)
        {
            base.EnterNode(ownerTree);
            Actor owneractor = ownerTree.OwnerAgent.Actor;
            TowerDefenseActorModule tdm = owneractor.GetModule<TowerDefenseActorModule>();
            
            if (tdm != null && !tdm.CanAct())
            {
                return BTNode.NodeStatus.FAILURE;
            }
            return BTNode.NodeStatus.SUCCESS;
        }
    }
}