using Kuantech.AI;
using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.TowerDefense
{
    /// <summary>
    /// A basic attack behaviour towards the enemy target
    /// </summary>
    public class BasicAttackAction : BTLeafAction
    {
        public string TargetKey = "EnemyActor";
        private bool _attacked = false;
        private Actor _enemyActor;
        public override void EnterNode(BehaviourTree ownerTree)
        {
            base.EnterNode(ownerTree);
            _attacked = false;
            _enemyActor = ownerTree.GetVariable<Actor>(TargetKey);
            if (_enemyActor == null) return;
            SurroundManager tm  = ownerTree.OwnerAgent.Actor.GetModule<SurroundManager>();
            tm.SetCurrentTarget(_enemyActor);
        }
        
        public override BTNode.NodeStatus Tick(BehaviourTree ownerTree)
        {
            if (_enemyActor == null || !_enemyActor.IsAlive() || !ownerTree.OwnerAgent.Actor.IsEnemy(_enemyActor))
            {
                return BTNode.NodeStatus.FAILURE;
            }
            
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
            
            //Is in attack range?
            WorldPoint hitPoint = _enemyActor.GetHitPoint(ownerActor);
            if (!cm.IsInAttackRange(hitPoint))
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