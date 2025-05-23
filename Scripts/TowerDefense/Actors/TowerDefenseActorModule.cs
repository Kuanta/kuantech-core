using Kuantech.AI.Pathfinding;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.TowerDefense
{
    public class TowerDefenseActorModule : ActorModule
    {
        [Header("Components")] 
        [SerializeField] private PathFollower PathFollower;

        [SerializeField] private float LateralOffsetMag;
        public void SetOnPath(Path path)
        {
            LateralOffsetMag = Mathf.Abs(LateralOffsetMag);
            PathFollower.FollowPath(path, Random.Range(-1*LateralOffsetMag, LateralOffsetMag));
        }
    }
}