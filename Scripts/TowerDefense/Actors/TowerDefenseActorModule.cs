using System;
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

        [NonSerialized] public bool CanAct; //Checks if the actor can act
        
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
        
        private void OnReachedEnd()
        {
            // Handle logic when the actor reaches the end of the path
            Debug.Log($"{Actor.name} has reached the end of the path.");
            // You can add more logic here, like triggering an event or destroying the actor.
            Actor.Despawn();
            
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

        public bool CanAttack()
        {
            float elapsedTime = Time.time - _lastActionTime;
            float attackSpeed = _statsModule.GetAttributeValue(AttackSpeedAttribute);
            if(elapsedTime < attackSpeed) return false;
            return true;
        }

        public void AttackEnemy()
        {
            Actor enemy = GetEnemyTarget();
            CombatModule combatModule = Actor.GetModule<CombatModule>();
            if (combatModule == null) return;
            combatModule.AttackToTarget(enemy);
        }
        
        #endregion
        #region Combat

        public override void ModuleUpdate()
        {
            base.ModuleUpdate();
            
            
        }

        private void AttackTarget()
        {
            TargetDetector.GetEnemyTarget();
        }

        public float GetDamage()
        {
            return _statsModule.GetAttributeValue(DamageAttribute);
        }
        #endregion
    }
}