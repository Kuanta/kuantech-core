using Kuantech.AI;
using Kuantech.Core;

namespace Kuantech.TowerDefense
{
    public class DetectTargetsAction : BTLeafAction
    {
        public override void EnterNode(BehaviourTree ownerTree)
        {
            base.EnterNode(ownerTree);
            Actor owneractor = ownerTree.OwnerAgent.Actor;
            TowerDefenseActorModule towerModule = owneractor.GetModule<TowerDefenseActorModule>();
            towerModule.TargetDetector.DetectTargets();
        }
    }
}