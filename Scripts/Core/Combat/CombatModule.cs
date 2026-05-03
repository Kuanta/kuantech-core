using System;
using System.Collections.Generic;
using Kuantech.Core.Combat;
using Kuantech.Core.FX;
using Kuantech.Core.Utils;
using Kuantech.Rpg;
using Kuantech.Rpg.Skills;
using Kuantech.Utils;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
#if NETWORKING_FISHNET
using FishNet.Object;
#endif

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
        [Tooltip("If true, attack implementation waits until the actor faces the attack direction before dealing damage.")]
        public bool WaitRotationalAlign = false;

        [Header("Attack Shape")]
        public AttackTypes AttackType;
        public bool IsMelee;
        public CombatVariable Angle;
        public CombatVariable Width;
        public CombatVariable Range;
        
        [Header("Required Resource")]
        public ResourceAsset RequiredResource;
        public float RequiredResourceAmount = 0;

        
        [Header("Damage")]
        public CombatDamageVariable Damage;
        public List<CombatDamageVariable> AdditionalDamages;
        
        [Header("Splash Damage")]
        public CombatDamageVariable SplashDamage;
        public List<CombatDamageVariable> AdditionalSplashDamages;
        
        public CombatVariable SplashRadius;
        
        
        [Header("Timings")] 
        public float AttackImplementationTime;
        public float EffectPlayTime;
        public float ContinuousAttackMaxTime;
        public bool ScaleAttackImplementationTimeWithAttackSpeed = true;
        public float AnimationTime;
        public float AttackDuration;
        public bool Continious; //Continious will attack every 'attack time' during the attack

        [Header("Attack Modifiers")] 
        public List<StatusEffectAsset> StatusEffectsToApply;
        
        [Header("Movement Manupilation")]
        public CombatVariable MovementSlow; //Factor between 0-1, movement speed while attacking will be MovementSpeed * (1-MovementSlow)

        [Header("Knosckback")]
        public CombatVariable Knockback;
        public CombatVariable KnockbackTime;
        
        [Header("Projectile")]
        public Projectile ProjectilePrefab;

        [Header("Skill")] 
        public SkillDataAsset SkillToCast;

        [Header("Animation")] 
        public AnimationData AttackAnimationData;

        [Header("FX")] 
        public EffectPlayer AttackFx = null;
        public bool SetAttackFxPosition = true;
        public bool SetAttackFxRotation = true;

        public EffectPlayer HitEffect = null;
        
        public DamageInfo GetDamageInfo(StatsModule statsModule)
        {
            return Damage.GetDamageInfo(statsModule);
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
        public LayerMask Targets;
        public LayerMask ObstacleLayerMask;

        [Header("Combo")]
        public float ComboRefreshTime = 1f; //Time in seconds to reset the combo

        [Header("Config")] 
        [SerializeField] public float RangeTolerance = 0.1f;
        
        //Locks & Cooldowns
        //public Cooldown GlobalCooldown;
        public LockVariable AttackLock = new LockVariable();
    
        //Events
        public UnityAction<CombatModule> AttackStartedEvent;
        public UnityAction<CombatModule> AttackedEvent; //Deals damage here
        public UnityAction<CombatModule> AttackCompletedEvent;


        //Quick module references
        private StatsModule _statModule;
        private AnimationModule _animationModule;
        private ActorSlotsHandler _slotsHandler;
        private HealthcareModule _healthcareModule;
        private SpellBook _spellBook;
        
        //Runtime
        private float _attackStartTime;
        private bool _isAttacking = false;
        private bool _attacked = false;
        private bool _requireAlignment = false;
        private bool _hasAligned = false;
        private float _lastAttackImplementationTime;
        private float _lastAttackCompleteTime;
        private bool _effectPlayed;
        private int _currentComboIndex;

        private ActionCastData _currentCastData;
        
        #region Lifecycle
        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            
            _statModule = Actor.GetModule<StatsModule>();
            _animationModule = Actor.GetModule<AnimationModule>();
            _slotsHandler = Actor.GetModule<ActorSlotsHandler>();
            _healthcareModule = Actor.GetModule<HealthcareModule>();
            _spellBook = Actor.GetModule<SpellBook>();
        }

        public override void ModuleUpdate()
        {
            if (!_isAttacking || AttackLock.IsLocked()) return;
            float elapsedTime = Time.time - _attackStartTime;
            AttackPattern currentPattern = GetCurrentAttackPattern();
            bool isNetworked = Networking.KtNetworkManager.IsNetworked();

            if (IsClientInitialized || !isNetworked)
            {
                if (elapsedTime >= _effectPlayTime && !_effectPlayed)
                {
                    PlayAttackFx();
                }
            }

            if (IsServerInitialized || !isNetworked)
            {
                if (_requireAlignment && !_hasAligned)
                    _hasAligned = HasAlignedWithAttackDirection();

                bool shouldImplement = elapsedTime >= _attackImplementationTime &&
                    (_hasAligned) &&
                    (!_attacked || (currentPattern.Continious && (Time.time - _lastAttackImplementationTime) >= _attackImplementationTime));
                if (shouldImplement && (elapsedTime <= _maxContinuousAttackTime || !currentPattern.Continious))
                {
                    RunAttackImplementation();
                    if (IsSpawned) ObserverAttackImplementation_Rpc();
                }

                if (elapsedTime > _attackDuration)
                {
                    EndAttack();
                }
            }
        }

        public override void Cleanup()
        {
            EndAttack();
        }
        #endregion

        #region Target
        
        /// <summary>
        /// Returns the targeted actor
        /// </summary>
        /// <returns></returns>
        public Actor GetCurrentTarget()
        {
            return null;
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
            return  CombatUtilities.GetAttackDuration(GetAttackSpeed(), GetBaseAttackTime(), MinAttackTime, MaxAttackTime);
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
            switch (GetCurrentAttackPattern().AttackType)
            {
                case AttackTypes.None:                                        break;
                case AttackTypes.Arc:             ArcAttack();                break;
                case AttackTypes.Arc2D:           ArcAttack2D();              break;
                case AttackTypes.Linear:          LinearAttack();             break;
                case AttackTypes.Linear2D:        LinearAttack2D();           break;
                case AttackTypes.Circle:          CircleAttack();             break;
                case AttackTypes.Circle2D:        CircleAttack2D();           break;
                case AttackTypes.Target:          TargetAttack();             break;
                case AttackTypes.RangedProjectile:
                case AttackTypes.TargetProjectile: RangedProjectileAttack(); break;
                case AttackTypes.SkillCast:       SkillCastAttack();          break;
                case AttackTypes.Beam:            BeamAttack();               break;
            }
            OnAttackImplemented();
        }

        private void OnAttackImplemented()
        {
            _attacked = true;
            _lastAttackImplementationTime = Time.time;
            AttackedEvent?.Invoke(this); //Maybe we shouldn't call this here
        }

        /// <summary>
        /// 3d arc attack
        /// </summary>
        private void ArcAttack()
        {
            AttackPattern currPattern = GetCurrentAttackPattern();
            List<Actor> actors = CombatUtilities.GetActorsInArc3D(
                GetAttackPosition(), GetAttackDirection().normalized,
                GetAttackRange(), currPattern.Angle.GetValue(_statModule),
                Targets, GetEnemyFactions());
            DamageActors(actors);
        }

        private void ArcAttack2D()
        {
            AttackPattern currPattern = GetCurrentAttackPattern();
            List<Actor> actors = CombatUtilities.GetActorsInArc2D(
                GetAttackPosition(), GetAttackDirection().normalized,
                GetAttackRange(), currPattern.Angle.GetValue(_statModule),
                Targets, GetEnemyFactions());
            DamageActors(actors);
        }

        private void LinearAttack()
        {
            AttackPattern currPattern = GetCurrentAttackPattern();
            List<Actor> actors = CombatUtilities.GetActorsInBox(
                GetAttackPosition(), GetAttackDirection().normalized,
                currPattern.Width.GetValue(_statModule), GetAttackRange(),
                Targets, GetEnemyFactions());
            DamageActors(actors);
        }

        private void LinearAttack2D()
        {
            AttackPattern currPattern = GetCurrentAttackPattern();
            List<Actor> actors = CombatUtilities.GetActorsInBox2D(
                GetAttackPosition(), GetAttackDirection().normalized,
                currPattern.Width.GetValue(_statModule), GetAttackRange(),
                Targets, GetEnemyFactions());
            DamageActors(actors);
        }

        private void CircleAttack()
        {
            List<Actor> actors = CombatUtilities.GetActorsInSphere(
                GetAttackPosition(), GetAttackRange(), Targets, GetEnemyFactions());
            DamageActors(actors);
        }

        private void CircleAttack2D()
        {
            List<Actor> actors = CombatUtilities.GetActorsInCircle2D(
                GetAttackPosition(), GetAttackRange(), Targets, GetEnemyFactions());
            DamageActors(actors);
        }
        
        /// <summary>
        /// Damages the target if in range
        /// </summary>
        public void TargetAttack()
        {
            Actor currentTarget = GetCurrentTarget();
            if (currentTarget == null ||
                !currentTarget.IsAlive()) return;

            if (!IsInAttackRange(currentTarget.GetHitPoint(Actor)))
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
            
            List<Actor> actors = CombatUtilities.GetActorsInRaycast2D(startPoint, direction, GetAttackRange(), Targets, GetEnemyFactions());
            DamageActors(actors);
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
            
            AttackPattern pattern = GetCurrentAttackPattern();
            projectile.IsVisualOnly = !IsServerInitialized;
            if (IsServerInitialized)
            {
                projectile.Damage = GetDamage();
                projectile.AdditionalDamages = GetAdditionalDamageInfos();
                projectile.SplashDamage = GetSplashDamage();
                projectile.AdditionalSplashDamages = GetAdditionalSplashDamages();
                projectile.SplashRadius = GetSplashDamageRadius();
                projectile.Knockback = pattern.Knockback.GetValue(_statModule);
                projectile.KnockbackTime = pattern.KnockbackTime.GetValue(_statModule);
            }
            projectile.Range = GetAttackRange();
             
            if (currentTarget != null)
            {
                Vector3 targetOffset = currentTarget.GetHitPoint(Actor).GetTargetPosition() - currentTarget.transform.position;
                projectile.Shoot(Actor, null, GetAttackPosition(), GetAttackDirection(), currentTarget.transform);
                projectile.SetTargetOffset(targetOffset);
            }
            else
            {
                projectile.Shoot(Actor, null, GetAttackPosition(), GetAttackDirection(), null);
            }
        }
        
        private void DamageActors(List<Actor> actors)
        {
            if(!IsServerInitialized) return; //Only server can
            List<Actor> hurtActors = new List<Actor>();
            foreach(var actor in actors)
            {
                if(ExecuteDamageActor(actor))
                {
                    hurtActors.Add(actor);
                }
            }
#if NETWORKING_FISHNET
            if(!IsSpawned || hurtActors.Count == 0) return;
            List<NetworkObject> nobs = new List<NetworkObject>(hurtActors.Count);
            foreach (var a in hurtActors) nobs.Add(a.GetComponent<NetworkObject>());
            ObserverDamageActors_Rpc(nobs);
#endif
        }

        /// <summary>
        /// Hits the actor with current damage parameters
        /// </summary>
        /// <param name="actor"></param>
        private void DamageActor(Actor actor)
        {
            if (!IsServerInitialized) return;
            bool hit = ExecuteDamageActor(actor);
#if NETWORKING_FISHNET
            if (hit && IsSpawned) ObserverDamageActor_Rpc(actor.GetComponent<NetworkObject>());
#endif
        }

        private bool ExecuteDamageActor(Actor actor)
        {
            if (actor == null || !actor.IsAlive()) return false;
            AttackPattern pattern = GetCurrentAttackPattern();

            HitInfo hitInfo = new HitInfo()
            {
                Hitter = gameObject,
                DamageInfo = GetDamage(),
                AdditionalDamages = GetAdditionalDamageInfos(),
                HitDirection = GetAttackDirection(),
                KnockbackForce = pattern.Knockback.GetValue(_statModule),
                KnockbackDuration = pattern.KnockbackTime.GetValue(_statModule)
            };
            if (IsServerInitialized)
            {
                
                //Apply status effects
                List<StatusEffectAsset> statusEffectDatas = GetCurrentAttackPattern().StatusEffectsToApply;
                if (!statusEffectDatas.IsNullOrEmpty())
                {
                    StatusEffectHandler seh = actor.GetModule<StatusEffectHandler>();
                    if (seh != null)
                    {
                        foreach (var statusEffectData in statusEffectDatas)
                        {
                            StatusEffect statusEffect = statusEffectData.CreateStatusEffect();
                            seh.AddStatusEffect(statusEffect);
                        }
                    }
                }
            }
            if(IsClientInitialized)
            {
                //Play hit effect
                EffectPlayer hitEffect = GetCurrentAttackPattern().HitEffect;
                if (hitEffect != null)
                {
                    Vector3 targetHitPoint = actor.GetHitPoint(Actor).GetTargetPosition();
                    Vector3 attackerPosition = Actor.transform.position;
                    attackerPosition.y = targetHitPoint.y;
                    hitEffect.PlayEffectAtPosition(actor.GetHitPoint(Actor).GetTargetPositionTowardsTarget(attackerPosition), Quaternion.LookRotation(GetAttackDirection()));
                }
            }
       
            actor.OnHit(hitInfo);
            return true;
        }
        
        private void SkillCastAttack()
        {
            AttackPattern currPattern = GetCurrentAttackPattern();
            if (currPattern.SkillToCast == null) return;
            if (_spellBook == null) return;
            ActionCastData skillCastData = new ActionCastData()
            {
                Direction = GetAttackDirection(),
                StartPosition = GetAttackPosition(),
                Target = GetCurrentTarget(),
            };
            _spellBook.CastSkill(currPattern.SkillToCast, skillCastData);
        }

  
#endregion

        #region Attack Pattern Queries

        public DamageInfo GetDamage()
            => GetCurrentAttackPattern().Damage.GetDamageInfo(_statModule);

        public List<DamageInfo> GetAdditionalDamageInfos()
        {
            var result = new List<DamageInfo>();
            foreach (var d in GetCurrentAttackPattern().AdditionalDamages)
                result.Add(d.GetDamageInfo(_statModule));
            return result;
        }

        public DamageInfo GetSplashDamage()
            => GetCurrentAttackPattern().SplashDamage.GetDamageInfo(_statModule);

        public List<DamageInfo> GetAdditionalSplashDamages()
        {
            var result = new List<DamageInfo>();
            foreach (var d in GetCurrentAttackPattern().AdditionalSplashDamages)
                result.Add(d.GetDamageInfo(_statModule));
            return result;
        }

        public float GetAttackRange()
            => GetCurrentAttackPattern().Range.GetValue(_statModule);

        public float GetSplashDamageRadius()
            => GetCurrentAttackPattern().SplashRadius.GetValue(_statModule);

        #endregion

        #region Attack Commands
        public bool Attack(ActionCastData castData)
        {
            bool canAttack = ExecuteAttack(castData);
            if (IsServerInitialized && IsSpawned)
            {
                if(canAttack)
                    ObserverAttackStart_Rpc(castData);
                else
                    ObserverAttackEnd_Rpc();
            }

            if(!IsServerInitialized && IsSpawned && canAttack)
                ServerAttack_Rpc(castData);
            return canAttack;
        }

        /// <summary>
        /// Executes attack
        /// </summary>
        /// <param name="castData"></param>
        /// <returns></returns>
        private bool ExecuteAttack(ActionCastData castData)
        {
            if(!CanAttack()) return false;

            //Server specific
            if(IsServerInitialized)
            {
                //Spend resource
                if (_healthcareModule != null)
                {
                    ResourceAsset resourceAsset = GetCurrentAttackPattern().RequiredResource;
                    if (resourceAsset != null)
                    {
                        _healthcareModule.RemoveResource(resourceAsset, GetCurrentAttackPattern().RequiredResourceAmount);
                    }
                }
            }

            _currentCastData = castData;
            _currentCastData.StartPosition = GetAttackPosition();

            // Turn towards attack direction and optionally wait for alignment
            AttackPattern currPatternForAlign = GetCurrentAttackPattern();
            _requireAlignment = currPatternForAlign.WaitRotationalAlign;
            _hasAligned = !_requireAlignment;
            if (castData.Target != null)
                Actor.MotionVectorsHandler.SetTargetObject(castData.Target.transform);
            else if (castData.Direction.sqrMagnitude > 0.01f)
                Actor.MotionVectorsHandler.SetTargetVector(castData.Direction);

            float timeSinceLastAttack = Time.time - _lastAttackCompleteTime;
            _currentComboIndex = timeSinceLastAttack < ComboRefreshTime ? _currentComboIndex + 1 : 0;

            //Common
            AttackPattern currPattern = GetCurrentAttackPattern();
            _isAttacking = true;
            _attacked = false;
            _attackStartTime = Time.time;
            _effectPlayed = false;
            _attackDuration = GetAttackDuration();
            _attackImplementationTime = GetAttackImplementationTime();
            _maxContinuousAttackTime = GetContinuousAttackMaxTime();
            _effectPlayTime = GetAttackFxPlayTime();


            bool isNetworked = Networking.KtNetworkManager.IsNetworked();
            if (IsClientInitialized || !isNetworked)
            {
                float timeMultiplier = Mathf.Max(0.01f, GetAttackSpeedMultiplier());
                float animationTime = currPattern.AnimationTime;
                animationTime = Mathf.Clamp(animationTime, 0, _attackDuration);
                if (_animationModule != null)
                {
                    _animationModule.PlayAnimationData(currPattern.AttackAnimationData, animationTime / timeMultiplier);
                }
            }

            AttackStartedEvent?.Invoke(this);
            return true;
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
                TargetPosition = startPosition + attackDireciton * GetAttackRange(),
            };
            return Attack(castData);
        }
        #endregion
        
        #region Queries

        public int GetCurrentComboIndex()
        {
            float timeSinceLastAttack = Time.time - _lastAttackCompleteTime;
            if (timeSinceLastAttack >= ComboRefreshTime)
            {
                return 0;
            }

            return _currentComboIndex;
        }
        public HashSet<int> GetEnemyFactions()
        {
            return Actor.FactionHandler.GetEnemyFactions().ToHashSet();
        }
        
        /// <summary>
        /// Checks if attack pattern is melee
        /// </summary>
        /// <returns></returns>
        public bool IsMelee()
        {
            AttackTypes attackType = GetCurrentAttackPattern().AttackType;
            if (attackType == AttackTypes.RangedProjectile || attackType == AttackTypes.RangedRaycast) return false;
            return GetCurrentAttackPattern().IsMelee;
        }
        
        /// <summary>
        /// Checks whether the actor can attack or not
        /// </summary>
        /// <returns></returns>
        public bool CanAttack()
        {
            if (_spellBook != null && _spellBook.IsCastingSkill()) return false;
            return CanUseAttack(GetCurrentAttackPattern());
        }
        
        /// <summary>
        /// Checks if an attack pattern can be used or not.
        /// </summary>
        /// <param name="attackPattern"></param>
        /// <returns></returns>
        public bool CanUseAttack(AttackPattern attackPattern)
        {
            if (_spellBook != null && _spellBook.IsCastingSkill()) return false;
            if (attackPattern.AttackType == AttackTypes.Target && GetCurrentTarget() == null) return false;
            if (!Actor.IsAlive() || AttackLock.IsLocked() || _isAttacking) return false;
            //Check resource
            if (_healthcareModule != null && attackPattern.RequiredResource != null)
            {
                float currentResource = _healthcareModule.GetCurrentResource(attackPattern.RequiredResource);
                if (currentResource < attackPattern.RequiredResourceAmount) return false;
            }
            return true;
        }
        
        public bool IsAttacking()
        {
            return _isAttacking;
        }
        
        /// <summary>
        /// Checks whether the target point is in attacking range
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool IsInAttackRange(WorldPoint target)
        {
            float dist = Vector3.Magnitude(target.GetTargetPosition() - Actor.GetActorLocation()) - target.Radius;
            return dist <= (GetAttackRange() + RangeTolerance);
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

        private void EndAttack()
        {
            if(IsServerInitialized)
            {
                ExecuteEndAttack();
                if (IsSpawned) ObserverAttackEnd_Rpc();
            }
            else
            {
                if (IsSpawned) ServerCancelAttack_Rpc();
                else ExecuteEndAttack(); // single player
            }
        }

        private void ExecuteEndAttack()
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
            EffectPlaySettings playSettings = EffectPlaySettings.GetPlayAtPositionSettings(attackPosition, Quaternion.LookRotation(attackDirection));
            playSettings.SetPosition = GetCurrentAttackPattern().SetAttackFxPosition;
            playSettings.SetRotation = GetCurrentAttackPattern().SetAttackFxRotation;
            playSettings.Caster = Actor;
            playSettings.ComboIndex = GetCurrentComboIndex();
            attackEffect.PlayEffect(playSettings);
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
            if (target != null)
            {
                Vector3 direction = target.transform.position - GetActionCastData().StartPosition;
                return direction.normalized;
            }
            ActionCastData castData =  GetActionCastData();
            if (castData.Direction.sqrMagnitude > 0.01f)
            {
                return castData.Direction;
            }

            return (castData.TargetPosition - GetAttackPosition()).normalized;
        }
        
        /// <summary>
        /// Returns the center position of attack. Projectiles will be cast from this, overlap attacks will center around this
        /// </summary>
        /// <returns></returns>
        private bool HasAlignedWithAttackDirection()
        {
            Vector3 targetVector = Actor.MotionVectorsHandler.GetTargetVector();
            if (targetVector.sqrMagnitude < 0.001f) return true;
            return Vector3.Dot(targetVector.normalized, Actor.transform.forward) >= 0.9f;
        }

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
                return getTarget.GetHitPoint(Actor).GetTargetPosition();
            }

            return startPosition + attackDireciton * GetAttackRange();
        }
        #endregion

        #region Networking
#if NETWORKING_FISHNET
        [ServerRpc]
        private void ServerAttack_Rpc(ActionCastData castData)
        {
           Attack(castData); //Is this correct?
        }

        [ServerRpc]
        private void ServerCancelAttack_Rpc()
        {
            ExecuteEndAttack();
            ObserverAttackEnd_Rpc();
        }

        [ObserversRpc(ExcludeOwner=true)]
        private void ObserverAttackStart_Rpc(ActionCastData castData)
        {
            if (IsServerInitialized) return; // listen server already ran ExecuteAttack
            ExecuteAttack(castData); //Let other clients start attack
        }

        [ObserversRpc]
        private void ObserverAttackImplementation_Rpc()
        {
            if (IsServerInitialized) return;
            RunAttackImplementation();
        }

        [ObserversRpc]
        private void ObserverAttackEnd_Rpc()
        {
            if (IsServerInitialized) return;
            ExecuteEndAttack();
        }

        [ObserversRpc]
        private void ObserverDamageActor_Rpc(NetworkObject target)
        {
            if (IsServerInitialized || target == null) return;
            if (target.TryGetComponent(out Actor actor)) ExecuteDamageActor(actor);
        }

        [ObserversRpc]
        private void ObserverDamageActors_Rpc(List<NetworkObject> targets)
        {
            if (IsServerInitialized) return;
            foreach (var target in targets)
            {
                if (target != null && target.TryGetComponent(out Actor actor))
                    ExecuteDamageActor(actor);
            }
        }
#else
        private void ServerAttack_Rpc(ActionCastData castData) { }
        private void ServerCancelAttack_Rpc() { }
        private void ObserverAttackStart_Rpc(ActionCastData castData) { }
        private void ObserverAttackImplementation_Rpc() { }
        private void ObserverAttackEnd_Rpc() { }
        private void ObserverDamageActor_Rpc(UnityEngine.GameObject target) { }
        private void ObserverDamageActors_Rpc(List<UnityEngine.GameObject> targets) { }
#endif
#endregion
    }


}


