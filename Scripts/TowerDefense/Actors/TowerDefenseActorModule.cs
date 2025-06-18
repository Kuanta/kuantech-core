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
        private MovementModule _movementModule;
        private float _lastActionTime;
        private TowerDefenseLevel _tdLevel;
        
        public override void Initialize()
        {
            if (Initialized) return;
            base.Initialize();
            PathFollower.OnReachedPathEnd += OnReachedEnd;
        }
        
        private void Update()
        {
            if (Actor == null) return;
            
            if(PathFollower == null || PathFollower.IsMoving())
            {
                Actor.MotionVectorsHandler.SetMovementVector(Vector3.zero);
            }
            if(PathFollower.IsMoving())
            {
                Actor.MotionVectorsHandler.SetMovementVector(PathFollower.GetMovementVector());
            }
        }
        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            _combatModule = Actor.GetModule<CombatModule>();
            _movementModule = Actor.GetModule<MovementModule>();
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
            //Is attacking?
            if(_combatModule != null && _combatModule.IsAttacking())
            {
                return false;
            }
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
            if (newState == ActorState.Spawned)
            {
                _tdLevel = LevelManager.GetCurrentLevel() as TowerDefenseLevel;
            }
            if(newState != ActorState.Spawned)
            {
                PathFollower.Stop();
            }
            if (newState == ActorState.Dead && _tdLevel != null)
            {
                _tdLevel.OnActorDeath(Actor);
            }

            if (newState == ActorState.Despawned && _tdLevel != null)
            {
                _tdLevel.OnActorDespawn(Actor);
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