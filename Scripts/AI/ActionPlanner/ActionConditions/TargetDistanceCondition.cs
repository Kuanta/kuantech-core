using Kuantech.Core;
using TotemHero;
using UnityEngine;

namespace Kuantech.TotemHero
{
    public class TargetDistanceCondition : ActorCondition
    {
        public DistanceEnums Distance;
        public override bool IsSatisfied(Actor owner)
        {
            EnemyActorModule eam = owner.GetModule<EnemyActorModule>();
            if(eam == null) return false;

            float distanceToTarget = Vector3.SqrMagnitude(owner.transform.position - eam.GetCurrentTargetPoint().GetTargetPosition());

            switch(Distance)
            {
                case DistanceEnums.Close:
                    return TotemHeroGameplayManager.IsCloseRange(distanceToTarget);
                case DistanceEnums.Mid:
                    return TotemHeroGameplayManager.IsMidRange(distanceToTarget);
                case DistanceEnums.Far:
                default:
                    return TotemHeroGameplayManager.IsLongRange(distanceToTarget);
            }
        }
    }
}