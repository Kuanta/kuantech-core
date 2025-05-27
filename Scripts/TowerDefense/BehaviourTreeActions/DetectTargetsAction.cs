using Kuantech.AI;
using Kuantech.Core;
using Kuantech.Core.Combat;

namespace Kuantech.TowerDefense
{
    public class DetectTargetsAction : BTLeafAction
    {
        public override BTNode.NodeStatus Tick(BehaviourTree ownerTree)
        {
            base.EnterNode(ownerTree);
            Actor owneractor = ownerTree.OwnerAgent.Actor;
            TargetDetectionModule tdm = owneractor.GetModule<TargetDetectionModule>();
            
            if (tdm != null)
            {
                tdm.DetectTargets();
                tdm.SortActors();
                Actor enemyActor = tdm.GetEnemyTarget();
                if (enemyActor != null)
                {
                    ownerTree.VariableTable.RegisterVariable("EnemyTarget", enemyActor);
                    return BTNode.NodeStatus.SUCCESS;

                }
            }
            ownerTree.VariableTable.RegisterVariable("EnemyTarget", null);
            return BTNode.NodeStatus.FAILURE;
        }

    }
}