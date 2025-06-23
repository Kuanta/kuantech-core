using Kuantech.AI;
using Kuantech.Core;

namespace Kuantech.TowerDefense
{
    /// <summary>
    /// A basic attack behaviour towards the enemy target
    /// </summary>
    public class BasicAttackAction : BTLeafAction
    {
        private bool _attacked = false;

        public override void EnterNode(BehaviourTree ownerTree)
        {
            base.EnterNode(ownerTree);
            _attacked = false;
        }
        public override BTNode.NodeStatus Tick(BehaviourTree ownerTree)
        {
            base.EnterNode(ownerTree);
            Actor ownerActor = ownerTree.OwnerAgent.Actor;
            CombatModule cm = ownerActor.GetModule<CombatModule>();

            if (cm == null) return BTNode.NodeStatus.FAILURE;
            
            //Is attack completed
            if (_attacked && !cm.IsAttacking())
            {
                return BTNode.NodeStatus.SUCCESS;
            }
            
            if (cm != null && cm.IsAttacking())
            {
                return BTNode.NodeStatus.RUNNING;
            }
            
            Actor actor = ownerTree.VariableTable.GetVariable("EnemyTarget") as Actor;
            if (actor == null || !actor.IsAlive())
            {
                return BTNode.NodeStatus.FAILURE;
            }
            
            cm.AttackToTarget(actor);
            _attacked = true;
            return BTNode.NodeStatus.RUNNING;
        }
    }
}