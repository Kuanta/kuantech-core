using System;
using System.Collections.Generic;
using Kuantech.Core.Combat;
using Kuantech.Core.Utils;
using Kuantech.Rpg;
using Kuantech.Rpg.Inventory;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using Cooldown = Kuantech.Rpg.Cooldown;

namespace Kuantech.Core
{
    public enum AttackTypes
    {
        None = 0,
        Linear,
        Arc,
        Circle,
        RangedProjectile, //For projecitle based attacks, like arrow and fireball
        RangedRaycast, //For raycast based attacks
        Target,
        TargetProjectile,
    }
    
    /// <summary>
    /// Set of parameters for a single attack parameter of a weapon
    /// </summary>
    [Serializable]
    public class AttackPattern
    {
        [Header("Attack Patterns")]
        public AttackTypes AttackType;
        public DamageInfo DamageInfo;
        public float Angle;
        public float Width;
        public float Range;

        
        [Header("Timings")]
        public float MovementSlow; //Factor between 0-1, movement speed while attacking will be MovementSpeed * (1-MovementSlow)
        public float WindupTime;
        public float AttackTime;
        public float AnimationTime;
        public float Cooldown;
        
        [Header("Knosckback")]
        public float Knockback;
        public float KnockbackTime;
        
        [Header("Projectile")]
        public Projectile ProjectilePrefab;
        public float ProjectileSpeed;
        public float ProjectileDrop;
        public bool TargetedProjectile;
        
        
        public DamageInfo GetDamageInfo()
        {
            return DamageInfo;
        }
    }

    public class CombatModule: ActorModule
    {
        [Header("Attack Pattern")]
        public AttackPattern DefaultAttackPattern;
        private AttackPattern _currentAttackPattern;

        //Target
        public Actor CurrentTarget;
        public Vector3 CurrentTargetPosition = Vector3.zero;

        [Header("Collision")]
        private Collider[] _results = new Collider[32];
        public LayerMask Targets;
        public LayerMask ObstacleLayerMask;
        private List<RaycastProjectile> _shotRaycastProjecitles = new List<RaycastProjectile>();
        
        [Header("Combo")]
        public float ComboRefreshTime = 1f; //Time in seconds to reset the combo

        //Locks & Cooldowns
        //public Cooldown GlobalCooldown;
        public LockVariable AttackLock = new LockVariable();
        public LockVariable SkillLock = new LockVariable();
        
   
    
        //Events
        public UnityAction<CombatModule> AttackStartedEvent;
        public UnityAction<CombatModule> AttackedEvent; //Deals damage here
        public UnityAction<CombatModule> AttackCompletedEvent;


        //Quick module references
        private AnimationModule _animationModule;
        private StatsModule _statModule;
        
        //Runtime
        private float _attackStartTime;
        private bool _isAttacking = false;
        private bool _attackWindupCompleted = false;
        private bool _attacked = false;
        private float _lastAttackTime;
        private int _attackIndex = 0;
        private Vector3 _attackDirection = Vector3.forward;
        private int _currentComboIndex;

        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            _animationModule = Actor.GetModule<AnimationModule>();
            _statModule = Actor.GetModule<StatsModule>();
        }

        private void Update()
        {
            if (!_isAttacking) return;
            float elapsedTime = Time.time - _attackStartTime;
            AttackPattern currentPattern = GetCurrentAttackPattern();
            if(elapsedTime > GetCurrentAttackPattern().WindupTime && !_attackWindupCompleted)
            {
                OnAttackWindupCompleted();
            }

            if (elapsedTime >= currentPattern.AttackTime && !_attacked)
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
                        TargetAttack();
                        break;
                    case AttackTypes.RangedProjectile:
                        RangedProjectileAttack();
                        break;
                }
                AttackedEvent?.Invoke(this);
            }

            if (elapsedTime > GetCurrentAttackPattern().Cooldown)
            {
                OnAttackCompleted();
            }
        }

        #region Attack Pattern Manupilation


        #endregion
        #region AttackPositions

        public Vector3 GetAttackPosition()
        {
            return transform.position;
        }

        #endregion
        
        #region Attack Implementations
                
        /// <summary>
        /// Damages the target if in range
        /// </summary>
        public void TargetAttack()
        {
            DamageInfo damage = GetCurrentAttackPattern().GetDamageInfo();
            if (CurrentTarget == null ||
                !CurrentTarget.IsAlive() ||
                Vector3.Distance(CurrentTarget.GetHitPoint().position, transform.position) > GetCurrentAttackPattern().Range) return;
            CurrentTarget.OnHit(gameObject, damage);
        }

        public void RangedProjectileAttack()
        {
            if (GetCurrentAttackPattern().ProjectilePrefab == null)
            {
                Debug.LogError("Projectile class is null but attack pattern is ranged projectile");
                return;
            }
            Projectile projectile = PoolManager.GetObjectFromPool(GetCurrentAttackPattern().ProjectilePrefab.gameObject).GetComponent<Projectile>();
            if (projectile == null) return;
            Vector3 targetOffset = CurrentTarget.GetHitPoint().position - CurrentTarget.transform.position;
            projectile.Shoot(this, null, GetAttackPosition(), _attackDirection, CurrentTarget.transform);
            projectile.SetTargetOffset(targetOffset);
        }
        #endregion

        #region Attack Pattern Queries

        public DamageInfo GetDamage()
        {
            return GetCurrentAttackPattern().GetDamageInfo();
        }
        
        #endregion

        #region Attack Commands
        public void Attack(Vector3 attackDirection, Actor target = null)
        {
            if (!CanAttack()) return;
            CurrentTarget = target;
            
            OnAttackStarted(attackDirection);

            float timeSinceLastAttack = _attackStartTime - _lastAttackTime;
            if (timeSinceLastAttack < ComboRefreshTime)
            {
                _currentComboIndex++;
            }
            else
            {
                _currentComboIndex = 0;
            }
            
            //todo(networking): Notify clients
        }

        public void AttackToTarget(Actor target)
        {
            Vector3 direciton = (target.transform.position - transform.position).normalized;
            Attack(direciton, target);
        }

        #endregion
        
        #region Queries
        /// <summary>
        /// Checks whether the actor can attack or not
        /// </summary>
        /// <returns></returns>
        public bool CanAttack()
        {
            if (GetCurrentAttackPattern().AttackType == AttackTypes.Target && CurrentTarget == null) return false;
            return !_isAttacking && !AttackLock.IsLocked() && Actor.IsAlive();
        }
        
        public bool IsAttacking()
        {
            return _isAttacking;
        }

        public bool IsInAttackRange(Transform target)
        {
            float sqrDist = Vector3.SqrMagnitude(target.position - Actor.transform.position);
            return sqrDist <= GetCurrentAttackPattern().Range * GetCurrentAttackPattern().Range;
        }
        
        public AttackPattern GetCurrentAttackPattern()
        {
            if (_currentAttackPattern == null) return DefaultAttackPattern;
            return _currentAttackPattern;
        }

        public void SetCurrentAttackPattern(AttackPattern attackPattern)
        {
            _currentAttackPattern = attackPattern;
        }
        #endregion
        
        #region Attack Lifecycle

        private void OnAttackStarted(Vector3 attackDirection)
        {
            _isAttacking = true;
            _attacked = false;
            _attackWindupCompleted = false;
            _attackStartTime = Time.time;
            _attackDirection = attackDirection;
            
            //todo(networking): Check server auth

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
            
            AttackStartedEvent?.Invoke(this);
        }

        private void OnAttackWindupCompleted()
        {
            _attackWindupCompleted = true;
            //If collider based melee, toggle weapon colliders
        }
        
        private void OnAttackCompleted()
        {
            _isAttacking = false;
            _lastAttackTime = Time.time;
            AttackCompletedEvent?.Invoke(this);
        }
        #endregion
        
    }
}