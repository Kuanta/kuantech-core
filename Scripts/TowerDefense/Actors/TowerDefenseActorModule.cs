using Kuantech.AI.Pathfinding;
using Kuantech.Core;
using Kuantech.Rpg;
using UnityEngine;

namespace Kuantech.TowerDefense
{
    public class TowerDefenseActorModule : ActorModule
    {
        [Header("Components")] 
        [SerializeField] private PathFollower PathFollower;
        [SerializeField] private float LateralOffsetMag;
        
        [Header("Movement Speed Attribute")]
        [SerializeField] private AttributeAsset SpeedAttribute;
        
        public override void Initialize()
        {
            if (Initialized) return;
            base.Initialize();
            PathFollower.OnReachedPathEnd += OnReachedEnd;
        }
        public void SetOnPath(Path path)
        {
            StatsModule sm  = Actor.GetModule<StatsModule>();
            if (sm != null && SpeedAttribute != null)
            {
                float speed = sm.GetAttributeValue(SpeedAttribute);
                PathFollower.SetFollowSpeed(speed);
            }
            LateralOffsetMag = Mathf.Abs(LateralOffsetMag);
            PathFollower.FollowPath(path, Random.Range(-1*LateralOffsetMag, LateralOffsetMag));
        }
        
        private void OnReachedEnd()
        {
            // Handle logic when the actor reaches the end of the path
            Debug.Log($"{Actor.name} has reached the end of the path.");
            // You can add more logic here, like triggering an event or destroying the actor.
            Actor.Despawn();
            
        }
    }
}