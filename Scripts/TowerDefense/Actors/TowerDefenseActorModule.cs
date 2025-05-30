using Kuantech.AI.Pathfinding;
using Kuantech.Core;
using Kuantech.Core.Combat;
using Kuantech.Rpg;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Kuantech.TowerDefense
{
    public class TowerDefenseActorModule : ActorModule
    {
        [Header("Components")] 
        [SerializeField] private PathFollower PathFollower;
        [SerializeField] private float LateralOffsetMag;
        [SerializeField] private TargetDetectionModule TargetDetector;
        
        [Header("Attributes")]
        [SerializeField] private AttributeAsset SpeedAttribute;
        [SerializeField] private AttributeAsset AttackSpeedAttribute;
        [SerializeField] private AttributeAsset DamageAttribute;
        
        
        private StatsModule _statsModule;
        private CombatModule _combatModule;
        private float _lastActionTime;
        
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

        public bool CanAct()
        {
            return Actor.CurrentActorState == ActorState.Spawned;
        }
        private void OnReachedEnd()
        {
            //Get td level
            TowerDefenseLevel tdLevel = LevelManager.GetCurrentLevel() as TowerDefenseLevel;
            if (tdLevel == null)
            {
                Debug.LogError("Tower defense level is null");
                return;
            }
            tdLevel.OnActorReachedEnd(Actor);
            Actor.Despawn();

        }

        public override void OnActorStateChanged(ActorState oldState, ActorState newState)
        {
            if(newState != ActorState.Spawned)
            {
                PathFollower.Stop();
            }
            
        }
        #region Targeting

        public void DetectTargets()
        {
            TargetDetector.DetectTargets();
        }

        public Actor GetEnemyTarget()
        {
            return TargetDetector.GetEnemyTarget();
        }

        public Actor GetAllyTarget()
        {
            return TargetDetector.GetAllyTarget();
        }
        #endregion
    }
}