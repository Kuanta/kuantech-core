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
        private Actor _enemyActor;
        public override void EnterNode(BehaviourTree ownerTree)
        {
            base.EnterNode(ownerTree);
            _attacked = false;
            TargetManager tm = ownerTree.OwnerAgent.Actor.GetModule<TargetManager>();
            if (tm == null)
            {
                _enemyActor = ownerTree.VariableTable.GetVariable<Actor>("EnemyTarget");
            }
            else
            {
                _enemyActor = tm.GetCurrentTarget();
            }
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
            
            if (_enemyActor == null || !_enemyActor.IsAlive())
            {
                return BTNode.NodeStatus.FAILURE;
            }

            if (!cm.AttackToTarget(_enemyActor))
            {
                return BTNode.NodeStatus.FAILURE;
            }
            _attacked = true;
            return BTNode.NodeStatus.RUNNING;
        }
    }
}