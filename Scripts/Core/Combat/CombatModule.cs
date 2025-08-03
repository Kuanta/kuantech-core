using System;
using System.Collections.Generic;
using Kuantech.Core.Combat;
using Kuantech.Core.FX;
using Kuantech.Core.Utils;
using Kuantech.Rpg;
using Kuantech.Rpg.Skills;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

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
        Linear2D,
        Arc2D,
        Circle2D,
        SkillCast, //Casts a skill, given in the attack pattern
        Beam,
    }
    
    /// <summary>
    /// Set of parameters for a single attack parameter of a weapon
    /// </summary>
    [Serializable]
    public class AttackPattern
    {
        [Header("Attack Point")] 
        public string AttackPointSlotName = "AttackPoint";
        
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
        public float AttackImplementationTime;
        public float EffectPlayTime;
        public float ContinuousAttackMaxTime;
        public bool ScaleAttackImplementationTimeWithAttackSpeed = true;
        public float AttackDuration;
        public bool Continious; //Continious will attack every 'attack time' during the attack
        
        [Header("Movement Manupilation")]
        public float MovementSlow; //Factor between 0-1, movement speed while attacking will be MovementSpeed * (1-MovementSlow)

        [Header("Knosckback")]
        public float Knockback;
        public float KnockbackTime;
        
        [Header("Projectile")]
        public Projectile ProjectilePrefab;

        [Header("Skill")] 
        public SkillDataAsset SkillToCast;
        
        [Header("Animation")]
        public AnimationData AttackAnimationData;

        [Header("FX")] 
        public EffectPlayer AttackFx = null;

        public EffectPlayer HitEffect = null;
        
        public DamageInfo GetDamageInfo()
        {
            return DamageInfo;
        }
    }

    public class CombatModule: ActorModule
    {
        [Header("Timings")] 
        public AttributeAsset AttackSpeedAttribute;
        public float MinAttackSpeed = 100.0f;
        public float MaxAttackSpeed = 1000.0f;
        public float MinAttackTime = 0.1f;
        public float MaxAttackTime = 50.0f;
        
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
        private ActorSlotsHandler _slotsHandler;
        
        //Runtime
        private float _attackStartTime;
        private bool _isAttacking = false;
        private bool _attacked = false;
        private float _lastAttackImplementationTime; //Last time an attack implementation has been called. Useful for continious attacks
        private float _lastAttackCompleteTime;
        private bool _effectPlayed;
        private int _attackIndex = 0;
        private int _currentComboIndex;

        private ActionCastData _currentCastData;
        
        public HashSet<int> FactionFilter = new HashSet<int>(); //Faction filter for the attack, if empty, all actors are valid targets

        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            
            _statModule = Actor.GetModule<StatsModule>();
            _animationModule = Actor.GetModule<AnimationModule>();
            _targetManager = Actor.GetModule<TargetManager>();
            _slotsHandler = Actor.GetModule<ActorSlotsHandler>();
        }


        private void Update()
        {
            if (!_isAttacking) return;
            float elapsedTime = Time.time - _attackStartTime;
            AttackPattern currentPattern = GetCurrentAttackPattern();

            if (elapsedTime >= _effectPlayTime && !_effectPlayed)
            {
                PlayAttackFx();
            }
            if (elapsedTime >= _attackImplementationTime && !_attacked || 
                (currentPattern.Continious &&  (Time.time - _lastAttackImplementationTime) >= _attackImplementationTime))
            {
                if (elapsedTime <= _maxContinuousAttackTime || !currentPattern.Continious)
                {
                    RunAttackImplementation();
                }
            }

            if (elapsedTime > _attackDuration)
            {
                OnAttackCompleted();
            }
        }

        #region Target
        
        /// <summary>
        /// Returns the targeted actor
        /// </summary>
        /// <returns></returns>
        public Actor GetCurrentTarget()
        {
            if (_targetManager == null) return null;
            return _targetManager.GetCurrentTarget();
        }
        #endregion

        #region Timings

        /// <summary>
        /// Returns the base attack time. 
        /// </summary>
        /// <returns></returns>
        public float GetBaseAttackTime()
        {
            return GetCurrentAttackPattern().AttackDuration;
        }
        
        /// <summary>
        /// Returns the attack speed
        /// </summary>
        /// <returns></returns>
        public virtual float GetAttackSpeed()
        {
            if (_statModule == null) return MinAttackSpeed;
            float attackSpeed = _statModule.GetAttributeValue(AttackSpeedAttribute);
            return Mathf.Clamp(attackSpeed, MinAttackSpeed, MaxAttackSpeed);
        }
        
        /// <summary>
        /// Returns the final attack duration. Formula is AttackSpeed/(100 x BaseAttackSpeed)
        /// </summary>
        /// <returns></returns>
        public float GetAttackDuration()
        {
            float attackSpeed = GetAttackSpeed();
            float bat = GetBaseAttackTime();
            float attackRate = attackSpeed / (100 * bat);
            float attackDuration = Mathf.Clamp(1 / attackRate, MinAttackTime, MaxAttackTime);
            return attackDuration;
        }
                
        /// <summary>
        /// Returns the multiplier calculated from BaseAttackTime/FinalAttackTime.
        /// Final attack time is calculated from GetAttackDuration.
        /// </summary>
        /// <returns></returns>
        public float GetAttackSpeedMultiplier()
        {
            float baseAttack = GetBaseAttackTime();
            float finalAttack = GetAttackDuration();
            return baseAttack / finalAttack;
        }

        /// <summary>
        /// Returns the attack fx play time
        /// </summary>
        /// <returns></returns>
        public float GetAttackFxPlayTime()
        {
            AttackPattern currPattern = GetCurrentAttackPattern();
            if (currPattern.ScaleAttackImplementationTimeWithAttackSpeed)
            {
                return GetCurrentAttackPattern().EffectPlayTime / GetAttackSpeedMultiplier();  
            }
            return GetCurrentAttackPattern().EffectPlayTime;
        }

        public float GetContinuousAttackMaxTime()
        {
            AttackPattern currPattern = GetCurrentAttackPattern();
            if (currPattern.ScaleAttackImplementationTimeWithAttackSpeed)
            {
                return currPattern.ContinuousAttackMaxTime / GetAttackSpeedMultiplier();    
            }

            return currPattern.ContinuousAttackMaxTime;
        }
        
        /// <summary>
        /// Returns the attack implementation time
        /// </summary>
        /// <returns></returns>
        public float GetAttackImplementationTime()
        {
            AttackPattern currPattern = GetCurrentAttackPattern();
            float attackSpeedMultiplier = GetAttackSpeedMultiplier();
            if (currPattern.ScaleAttackImplementationTimeWithAttackSpeed && attackSpeedMultiplier > 0)
            {
                return currPattern.AttackImplementationTime / attackSpeedMultiplier;    
            }

            return currPattern.AttackImplementationTime;
        }
        #endregion


        #region Attack Implementations

        private void RunAttackImplementation()
        {
            _attacked = true;
            switch (GetCurrentAttackPattern().AttackType)
            {
                case AttackTypes.None:
                    break;
                case AttackTypes.Arc:
                    ArcAttack();
                    break;
                case AttackTypes.Arc2D:
                    ArcAttack2D();
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
                case AttackTypes.SkillCast:
                    SkillCastAttack();
                    break;
                case AttackTypes.Beam:
                    BeamAttack();
                    break;
            }

            _lastAttackImplementationTime = Time.time;
            AttackedEvent?.Invoke(this); //Maybe we shouldn't call this here
        }

        public void ArcAttack()
        {
            AttackPattern currPattern = GetCurrentAttackPattern();
            Vector3 attackPoint = GetAttackPosition();
            Vector3 forward = GetAttackDirection().normalized;
            float range = currPattern.Range;
            float angle = currPattern.Angle;
            
        }

        public void ArcAttack2D()
        {
            AttackPattern currPattern = GetCurrentAttackPattern();
            Vector3 attackPoint = GetAttackPosition();
            Vector3 forward = GetAttackDirection().normalized;
            float range = currPattern.Range;
            float angle = currPattern.Angle;
            List<Actor> actors = CombatUtilities.GetActorsInArc2D(attackPoint, forward, range, angle, Targets, FactionFilter);

            foreach (var actor in actors)
            {
                DamageActor(actor);
            }
        }
        
        /// <summary>
        /// Damages the target if in range
        /// </summary>
        public void TargetAttack()
        {
            Actor currentTarget = GetCurrentTarget();
            if (currentTarget == null ||
                !currentTarget.IsAlive()) return;

            if (!IsInAttackRange(currentTarget.GetHitPoint().transform))
            {
                return;
            }
            
            DamageActor(currentTarget);
        }
        
        private void BeamAttack()
        {
            Vector3 startPoint = GetAttackPosition();
            Vector3 direction = GetAttackDirection();
            AttackPattern attackPattern = GetCurrentAttackPattern();
            
            List<Actor> actors = CombatUtilities.GetActorsInRaycast2D(startPoint, direction, attackPattern.Range, Targets, FactionFilter);
            foreach (var actor in actors)
            {
                DamageActor(actor);
            }
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
            
            //Apply projectileproperties
            projectile.Damage = GetDamage();
            projectile.Range = GetCurrentAttackPattern().Range;
            projectile.Knockback = GetCurrentAttackPattern().Knockback;
            projectile.KnockbackTime = GetCurrentAttackPattern().KnockbackTime;
            
            if (currentTarget != null)
            {
                Vector3 targetOffset = currentTarget.GetHitPoint().position - currentTarget.transform.position;
                projectile.Shoot(Actor, null, GetAttackPosition(), GetAttackDirection(), currentTarget.transform);
                projectile.SetTargetOffset(targetOffset);
            }
            else
            {
                projectile.Shoot(Actor, null, GetAttackPosition(), GetAttackDirection(), null);
            }
            
        }
        
        /// <summary>
        /// Hits the actor with current damage parameters
        /// </summary>
        /// <param name="actor"></param>
        private void DamageActor(Actor actor)
        {
            DamageInfo damage = GetDamage();
            if (actor == null || !actor.IsAlive()) return;
            HitInfo hitInfo = new HitInfo()
            {
                Hitter = gameObject,
                DamageInfo = damage,
                HitDirection = GetAttackDirection(),
                KnockbackForce = GetCurrentAttackPattern().Knockback,
                KnockbackDuration = GetCurrentAttackPattern().KnockbackTime
            };
            //Play hit effect
            EffectPlayer hitEffect = GetCurrentAttackPattern().HitEffect;
            if (hitEffect != null)
            {
                hitEffect.PlayEffectAtPosition(actor.GetHitPoint().position, Quaternion.LookRotation(GetAttackDirection()));
            }
            actor.OnHit(hitInfo);
        }

        private void SkillCastAttack()
        {
            AttackPattern currPattern = GetCurrentAttackPattern();
            if (currPattern.SkillToCast == null) return;
            SpellBook spellBook = Actor.GetModule<SpellBook>();
            if (spellBook == null) return;
            SkillCastData skillCastData = new SkillCastData()
            {
                CastDirection = GetAttackDirection(),
                CastStartPosition = GetAttackPosition(),
                CastTarget = GetCurrentTarget(),
            };
            spellBook.CastSkill(currPattern.SkillToCast, skillCastData);
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
        public bool Attack(ActionCastData castData)
        {
            if (!CanAttack()) return false;
            _currentCastData = castData;
            if (_currentCastData.Target != null && _targetManager != null)
            {
                _targetManager.SetCurrentTarget(_currentCastData.Target);
            }

            _currentCastData.StartPosition = GetAttackPosition();

            Actor.MotionVectorsHandler.SetTargetVector(_currentCastData.Direction);
            
            OnAttackStarted(castData);

            float timeSinceLastAttack = _attackStartTime - _lastAttackCompleteTime;
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
        
        /// <summary>
        /// Attacks to a target
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool AttackToTarget(Actor target)
        {
            Vector3 direciton = (target.transform.position - transform.position).normalized;
            ActionCastData castData = new ActionCastData
            {
                Caster = Actor,
                StartPosition = GetAttackPosition(),
                Direction = direciton,
                Target = target,
                TargetPosition = target.transform.position
            };
            
            return Attack(castData);
        }
        
        /// <summary>
        /// Attacks to a position
        /// </summary>
        /// <param name="attackPosition"></param>
        /// <returns></returns>
        public bool AttackToPosition(Vector3 attackPosition)
        {
            Vector3 direciton = (attackPosition - transform.position).normalized;
            ActionCastData castData = new ActionCastData
            {
                Caster = Actor,
                StartPosition = GetAttackPosition(),
                Direction = direciton,
                Target = null,
                TargetPosition = attackPosition
            };
            
            return Attack(castData);
        }

        public bool AttackToDirection(Vector3 attackDireciton)
        {
            Vector3 startPosition = GetAttackPosition();
            attackDireciton = attackDireciton.normalized;
            ActionCastData castData = new ActionCastData()
            {
                Caster = Actor,
                StartPosition = startPosition,
                Direction = attackDireciton.normalized,
                Target = null,
                TargetPosition = startPosition + attackDireciton * GetCurrentAttackPattern().Range,
            };
            return Attack(castData);
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
            if (attackPattern.AttackDuration < attackPattern.AttackImplementationTime) attackPattern.AttackDuration = attackPattern.AttackImplementationTime;
            _currentAttackPattern = attackPattern;
        }
        #endregion
        
        #region Attack Lifecycle
        private float _effectPlayTime;
        private float _attackDuration;
        private float _attackImplementationTime;
        private float _maxContinuousAttackTime;

        private void OnAttackStarted(ActionCastData castData)
        {
            AttackPattern currPattern = GetCurrentAttackPattern();
            //Set timings
            _attackDuration = GetAttackDuration();
            float timeMultiplier = GetAttackSpeedMultiplier();
            _effectPlayTime = GetAttackFxPlayTime();
            _attackImplementationTime = GetAttackImplementationTime();
            _maxContinuousAttackTime = GetContinuousAttackMaxTime();
            
            _isAttacking = true;
            _attacked = false;
            _attackStartTime = Time.time;
            _effectPlayed = false;
            if(_animationModule != null)
            {
                _animationModule.PlayAnimation(currPattern.AttackAnimationData, timeMultiplier);
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

        private void OnAttackCompleted()
        {
            _isAttacking = false;
            _lastAttackCompleteTime = Time.time;
            AttackCompletedEvent?.Invoke(this);
        }
        
        #endregion

        #region Fx
        
        /// <summary>
        /// Plays a simple attack fx
        /// </summary>
        public void PlayAttackFx()
        {
            if (_effectPlayed) return;
            _effectPlayed = true;
            Vector3 attackDirection = GetAttackDirection();
            Vector3 attackPosition = GetAttackPosition(); //Position where attack is starterd, casted
            EffectPlayer attackEffect = GetCurrentAttackPattern().AttackFx;
            if (attackEffect == null) return;
            attackEffect.PlayEffectAtPosition(attackPosition, Quaternion.LookRotation(attackDirection));
        }
        
        #endregion

        #region CastData
            
        /// <summary>
        /// Returns the current attack pattern
        /// </summary>
        /// <returns></returns>
        public ActionCastData GetActionCastData()
        {
            return _currentCastData;
        }
        
        public Vector3 GetAttackDirection()
        {
            Actor target = GetCurrentTarget();
            if(target == null) return GetActionCastData().Direction;
            Vector3 direction = target.transform.position - GetActionCastData().StartPosition;
            return direction.normalized;
        }
        
        /// <summary>
        /// Returns the center position of attack. Projectiles will be cast from this, overlap attacks will center around this
        /// </summary>
        /// <returns></returns>
        public Vector3 GetAttackPosition()
        {
            string slotName = GetCurrentAttackPattern().AttackPointSlotName;
            if (_slotsHandler != null)
            {
                Transform slot = _slotsHandler.GetSlot(slotName);
                if (slot != null)
                {
                   return slot.position;
                }
            }
            return transform.position;
        }

        public Vector3 GetTargetPosition()
        {
            Actor getTarget = GetCurrentTarget();
            Vector3 startPosition = GetAttackPosition();
            Vector3 attackDireciton = GetAttackDirection();
            if(getTarget != null)
            {
                return getTarget.GetHitPoint().position;
            }

            return startPosition + attackDireciton * GetCurrentAttackPattern().Range;
        }
        #endregion
        
    }
}