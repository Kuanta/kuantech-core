using System;
using System.Collections.Generic;
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
        [Header("Attack Pattern")]
        public WeaponAttackPattern DefaultAttackPattern;
        [NonSerialized] public WeaponAttackPattern CurrentAttackPattern;

        //Target
        public Actor CurrentTarget;
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
        private float _attackStartTime;
        private bool _isAttacking = false;
        private bool _attackWindupCompleted = false;
        private bool _attacked = false;
        private float _lastAttackTime;
        private int _attackIndex = 0;
        private Vector3 _attackDirection = Vector3.forward;
        private int _lastComboIndex;
    
        //Events
        private UnityAction AttackCompleteHandler;


        //Quick module references
        private AnimationModule _animationModule;
        private StatsModule _statModule;

        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            _animationModule = Actor.GetModule<AnimationModule>();
            _statModule = Actor.GetModule<StatsModule>();
            Refresh();
            
        }

        private void Update()
        {
            if (!_isAttacking) return;
            float elapsedTime = Time.time - _attackStartTime;
            WeaponAttackPattern currentPattern = GetCurrentAttackPattern();
            if(elapsedTime > GetCurrentAttackPattern().WindupTime && !_attackWindupCompleted)
            {
                OnAttackWindupCompleted();
            }

            if (Time.time - _attackStartTime >= currentPattern.DamageTime && !_attacked)
            {
                _attacked = true;
                switch (currentPattern.AttackType)
                {
                    case AttackTypes.None:
                        break;
                    case AttackTypes.Arc:
                        break;
                    case AttackTypes.Linear:
                        break;
                    case AttackTypes.Circle:
                        break;
                    case AttackTypes.Target:
                        break;
                    case AttackTypes.Ranged:
                        break;
                }
            }
        }
        
        #region CastSkill
        public bool CanUseSkill()
        {
            return GlobalCooldown.IsOffCooldown() && !SkillLock.IsLocked() && Actor.IsAlive();
        }

        public void CastSkill(Skill skill)
        {
            if(!CanUseSkill()) return;

            //Check skill resources
        }
        #endregion

        #region Attack Pattern Queries

        public DamageInfo GetDamage()
        {
            return CurrentAttackPattern.GetDamageInfo();
        }
        
        #endregion
        
        #region Attacks

        public void Attack(Vector3 attackDirection)
        {
            if (!CanAttack()) return;
            OnAttackStarted(attackDirection);

            float timeSinceLastAttack = _attackStartTime - _lastAttackTime;
        }

        private void OnAttackStarted(Vector3 attackDirection)
        {
            _isAttacking = true;
            _attacked = false;
            _attackWindupCompleted = false;
            _attackStartTime = Time.time;
            _attackDirection = attackDirection;
            
            //todo: Check server auth

            if (Actor.GetModule<MovementModule>())
            {
                //todo: Transform this UE code to unity
                // if(!ParentActor->HasAuthority()) return;
                // if(ParentActor->MovementModule == nullptr) return;
                // attackDirection.Z = 0;
                // attackDirection.Normalize();
	               //
                // if(_currentAttackPattern.ForwardMovementSpeed > 0.0f)
                // {
                //     ParentActor->MovementModule->SetForceMovementVector(attackDirection * _currentAttackPattern.ForwardMovementSpeed, true);
                // }
                // ParentActor->MovementModule->MovementSpeedScale = _currentAttackPattern.MovementSlow;
                // if(_currentAttackPattern.ForceTurn)
                // {
                //     ParentActor->MovementModule->SetRotationWithDirection(attackDirection, true, true);
                // }
                // if(_currentAttackPattern.LockRotation) ParentActor->MovementModule->RotationLock.Lock();
            }
        }
        
        public bool IsAttacking()
        {
            return _isAttacking;
        }

        public bool IsInAttackRange(Transform target)
        {
            float sqrDist = Vector3.SqrMagnitude(target.position - Actor.transform.position);
            return sqrDist <= CurrentAttackPattern.Range * CurrentAttackPattern.Range;
        }
        public WeaponAttackPattern GetCurrentAttackPattern()
        {
            if (CurrentAttackPattern == null) return DefaultAttackPattern;
            return CurrentAttackPattern;
        }
        
        /// <summary>
        /// Checks whether the actor can attack or not
        /// </summary>
        /// <returns></returns>
        public bool CanAttack()
        {
            if (CurrentAttackPattern.AttackType == AttackTypes.Target && CurrentTarget == null) return false;
            return !_isAttacking && GlobalCooldown.IsOffCooldown() && !AttackLock.IsLocked() && Actor.IsAlive();
        }
        
        //==== Attack Implementations ====
        /// <summary>
        /// Damages the target if in range
        /// </summary>
        public void TargetAttack()
        {
            DamageInfo damage = GetCurrentAttackPattern().GetDamageInfo();
            if (CurrentTarget == null ||
                !CurrentTarget.IsAlive() ||
                Vector3.Distance(CurrentTarget.transform.position, transform.position) > CurrentAttackPattern.Range) return;
            CurrentTarget.OnHit(gameObject, damage);
        }

        #endregion
        
        #region Events

        private void OnAttackWindupCompleted()
        {
            _attackWindupCompleted = true;
            
            //If collider based melee, toggle weapon colliders
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