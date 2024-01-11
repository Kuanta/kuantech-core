using System;
using System.Collections.Generic;
using Kuantech.ArcadeIdle;
using Kuantech.Core.Utils;
using Kuantech.Rpg;
using Kuantech.Rpg.Inventory;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Core.Combat
{
    public enum AttackTypes
    {
        None = 0,
        Linear,
        Arc,
        Circle,
        Ranged, //For projecitle based attacks, like arrow and fireball
        RangedRaycast, //For raycast based attacks
        Target,
        TargetProjectile,
    }
    public class CombatModule: ActorModule
    {
        [Header("Combat Resources")]
        [SerializeField] private CombatResourceData HealthResourceData;
        [NonSerialized] private CombatResource Health;
        [SerializeField] private List<CombatResourceData> CombatResourcesList;
        private Dictionary<CombatResourceData, CombatResource> _combatResources;

        [Header("Attack Pattern")]
        public WeaponAttackPattern DefaultAttackPattern;
        public WeaponAttackPattern CurrentAttackPattern;

        //Target
        public CombatModule CurrentTarget;
        public Vector3 CurrentTargetPosition = Vector3.zero;

        [Header("Collision")]
        private Collider[] _results = new Collider[32];
        public LayerMask Targets;
        public LayerMask ObstacleLayerMask;
        private List<RaycastProjectile> _shotRaycastProjecitles = new List<RaycastProjectile>();

        //Locks & Cooldowns
        public Cooldown GlobalCooldown;
        public LockVariable AttackLock = new LockVariable();
        public LockVariable SkillLock = new LockVariable();

        //States
        public float AttackStartTime;
        public bool IsAttacking = false;
        private bool _damageDone = false; //Checks if DamageTime has passed

        //Events
        public EventHandler OnDeath;
        private UnityAction AttackCompleteHandler;


        //Quick module references
        private ActorAnimationModule _animatorModule;
        private StatsModule _statModule;

        //Animation  hashes

        public override void Initialize()
        {
            base.Initialize();
            _combatResources = new Dictionary<CombatResourceData, CombatResource>();
            foreach(var res in CombatResourcesList)
            {
                _combatResources[res] = new CombatResource()
                {
                    MaxValueAttribute = res.MaxValueAttribute,
                    RegenAttribute = res.RegenAttribute,
                };
            }
            Health = new CombatResource()
            {
                MaxValueAttribute = HealthResourceData.MaxValueAttribute,
                RegenAttribute = HealthResourceData.MaxValueAttribute,
            };
        }
        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            _animatorModule = Actor.GetModule<ActorAnimationModule>();
            _statModule = Actor.GetModule<StatsModule>();
            Refresh();
            
        }
        
        #region Stats
        public DamageData GetDamage()
        {
            return new DamageData()
            {
                Damage = 0f, //haha
            };
        }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Receives damage if actor is still alive
        /// </summary>
        /// <param name="damageData"></param>
        public void ReceiveDamage(CombatModule from, DamageData damageData)
        {
            if(!IsAlive()) return;
            //Reduce damage 
            Health.Remove(GetDamage().Damage);
            if(Health.CurrentValue <= 0)
            {
                Death();
            }
        }

        /// <summary>
        /// Checks whether the actor is alive
        /// </summary>
        /// <returns></returns>
        public bool IsAlive()
        {
            return Health.CurrentValue > 0;
        }

        public void Respawn()
        {

        }

        /// <summary>
        /// Called once the current health reaches to zero
        /// </summary>
        public void Death()
        {
            //Play death animation

            //Trigger death event
            OnDeath?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region CastSkill
        public bool CanUseSkill()
        {
            return GlobalCooldown.IsOffCooldown() && !SkillLock.IsLocked() && IsAlive();
        }

        public void CastSkill(Skill skill)
        {
            if(!CanUseSkill()) return;

            //Check skill resources
        }
        #endregion

        #region Attacks
        /// <summary>
        /// Checks whether the actor can attack or not
        /// </summary>
        /// <returns></returns>
        public bool CanAttack()
        {
            if (CurrentAttackPattern.AttackType == AttackTypes.Target && CurrentTarget == null) return false;
            return !IsAttacking && GlobalCooldown.IsOffCooldown() && !AttackLock.IsLocked() && IsAlive();
        }

        /// <summary>
        /// Damages the target if in range
        /// </summary>
        public void TargetAttack()
        {
            DamageData damage = GetDamage();
            CombatModule target = CurrentTarget;
            if (target == null ||
                !target.IsAlive() ||
                Vector3.Distance(target.transform.position, transform.position) > CurrentAttackPattern.Range) return;

            target.ReceiveDamage(this, damage);
        }

        #endregion

        /// <summary>
        /// Refreshes the state, resources, etc.
        /// </summary>
        public void Refresh()
        {
            foreach(var pair in _combatResources)
            {
                pair.Value.Refresh(_statModule);
            }
        }
    }
}