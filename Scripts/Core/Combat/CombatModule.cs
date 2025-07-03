using System;
using System.Collections.Generic;
using Kuantech.Core.Utils;
using Kuantech.Rpg;
using UnityEngine;
using UnityEngine.Events;

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
        [Header("Attack Shape")]
        public AttackTypes AttackType;
        public float Angle;
        public float Width;
        public float Range;
        
        [Header("Damage")]
        public DamageInfo DamageInfo;
        public AttributeAsset AttributeToScaleDamage;
        public float AttributeScaleFactor = 0f; //How much the attribute value scales the damage, 1 means 1:1 scaling
        
        [Header("Timings")]
        public float MovementSlow; //Factor between 0-1, movement speed while attacking will be MovementSpeed * (1-MovementSlow)
        public float WindupTime;
        public float AttackTime;
        public float Cooldown;
        
        [Header("Knosckback")]
        public float Knockback;
        public float KnockbackTime;
        
        [Header("Projectile")]
        public Projectile ProjectilePrefab;
        
        [Header("Animation")]
        public AnimationData AttackAnimationData;
        
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

        [Header("Collision")]
        private Collider[] _results = new Collider[32];
        public LayerMask Targets;
        public LayerMask ObstacleLayerMask;
        private List<RaycastProjectile> _shotRaycastProjecitles = new List<RaycastProjectile>();
        
        [Header("Combo")]
        public float ComboRefreshTime = 1f; //Time in seconds to reset the combo

        [Header("Config")] 
        [SerializeField] public float RangeTolerance = 0.1f;
        
        //Locks & Cooldowns
        //public Cooldown GlobalCooldown;
        public LockVariable AttackLock = new LockVariable();
        public LockVariable SkillLock = new LockVariable();
        
    
        //Events
        public UnityAction<CombatModule> AttackStartedEvent;
        public UnityAction<CombatModule> AttackedEvent; //Deals damage here
        public UnityAction<CombatModule> AttackCompletedEvent;


        //Quick module references
        private StatsModule _statModule;
        private TargetManager _targetManager;
        private AnimationModule _animationModule;
        
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
            
            _statModule = Actor.GetModule<StatsModule>();
            _animationModule = Actor.GetModule<AnimationModule>();
            _targetManager = Actor.GetModule<TargetManager>();
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

        #region Target
        
        public Actor GetCurrentTarget()
        {
            if (_targetManager == null) return null;
            return _targetManager.GetCurrentTarget();
        }
        #endregion
        
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
            DamageInfo damage = GetDamage();
            Actor currentTarget = GetCurrentTarget();
            if (currentTarget == null ||
                !currentTarget.IsAlive()) return;

            if (!IsInAttackRange(currentTarget.GetHitPoint().transform))
            {
                return;
            }
            
            HitInfo hitInfo = new HitInfo()
            {
                Hitter = gameObject,
                DamageInfo = damage,
                HitDirection = _attackDirection,
                KnockbackForce = GetCurrentAttackPattern().Knockback,
                KnockbackDuration = GetCurrentAttackPattern().KnockbackTime
            };
             currentTarget.OnHit(hitInfo);
        }

        public void RangedProjectileAttack()
        {
            if (GetCurrentAttackPattern().ProjectilePrefab == null)
            {
                Debug.LogError("Projectile class is null but attack pattern is ranged projectile");
                return;
            }
            Actor currentTarget = GetCurrentTarget();
            Projectile projectile = PoolManager.GetObjectFromPool(GetCurrentAttackPattern().ProjectilePrefab.gameObject).GetComponent<Projectile>();
            if (projectile == null) return;
            if (currentTarget != null)
            {
                Vector3 targetOffset = currentTarget.GetHitPoint().position - currentTarget.transform.position;
                projectile.Shoot(this, null, GetAttackPosition(), _attackDirection, currentTarget.transform);
                projectile.SetTargetOffset(targetOffset);
            }
            else
            {
                projectile.Shoot(this, null, GetAttackPosition(), _attackDirection, null);
            }
            
        }
        #endregion

        #region Attack Pattern Queries

        public DamageInfo GetDamage()
        {
            AttackPattern attackPattern = GetCurrentAttackPattern();
            DamageInfo damageInfo = attackPattern.GetDamageInfo();
            float statVariable = _statModule.GetAttributeValue(attackPattern.AttributeToScaleDamage);
            damageInfo.DamageAmount += statVariable * attackPattern.AttributeScaleFactor;
            return damageInfo;
        }
        
        #endregion

        #region Attack Commands
        public bool Attack(Vector3 attackDirection, Actor target = null)
        {
            if (!CanAttack()) return false;
            if (target != null && _targetManager != null)
            {
                _targetManager.SetCurrentTarget(target);
            }
            
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

            return true;
            //todo(networking): Notify clients
        }

        public bool AttackToTarget(Actor target)
        {
            Vector3 direciton = (target.transform.position - transform.position).normalized;
            return Attack(direciton, target);
        }

        #endregion
        
        #region Queries
        /// <summary>
        /// Checks whether the actor can attack or not
        /// </summary>
        /// <returns></returns>
        public bool CanAttack()
        {
            if (GetCurrentAttackPattern().AttackType == AttackTypes.Target && GetCurrentTarget() == null) return false;
            return !_isAttacking && !AttackLock.IsLocked() && Actor.IsAlive();
        }
        
        public bool IsAttacking()
        {
            return _isAttacking;
        }

        public bool IsInAttackRange(Transform target)
        {
            float sqrDist = Vector3.SqrMagnitude(target.position - Actor.transform.position);
            return sqrDist <= (GetCurrentAttackPattern().Range + RangeTolerance) * (GetCurrentAttackPattern().Range + RangeTolerance);
        }
        
        public AttackPattern GetCurrentAttackPattern()
        {
            if (_currentAttackPattern == null) return DefaultAttackPattern;
            return _currentAttackPattern;
        }

        public void SetCurrentAttackPattern(AttackPattern attackPattern)
        {
            if (attackPattern.Cooldown < attackPattern.AttackTime) attackPattern.Cooldown = attackPattern.AttackTime;
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
            
            if(_animationModule != null)
            {
                _animationModule.PlayAnimation(GetCurrentAttackPattern().AttackAnimationData);
            }
            else
            {
                Debug.LogWarning("CombatModule: AnimationModule is null, cannot set attack animation.");            
            }
            //todo(networking): Check server auth

            if (Actor.GetModule<RigidbodyMovementModule>())
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