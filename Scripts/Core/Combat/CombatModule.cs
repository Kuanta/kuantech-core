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
#if NETWORKING_NGO
using Unity.Netcode;
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

        #region Attributr Based Variables
        [Header("Damage")]
        public AtributeBasedDamageVariable Damage;
        public List<AtributeBasedDamageVariable> AdditionalDamages;

        [Header("Splash Damage")]
        public AtributeBasedDamageVariable SplashDamage;
        public List<AtributeBasedDamageVariable> AdditionalSplashDamages;

        [Header("hape")]
        public AttributeBasedVariable SplashRadius;
        public AttributeBasedVariable Angle;
        public AttributeBasedVariable Width;
        public AttributeBasedVariable Range;
        public CombatIndicator.CombatIndicatorType IndicatorType = CombatIndicator.CombatIndicatorType.NONE;

        [Header("Knockback")]
        public AttributeBasedVariable Knockback;
        public AttributeBasedVariable KnockbackTime;

        #endregion


        [Header("Required Resource")]
        public ResourceAsset RequiredResource;
        public float RequiredResourceAmount = 0;
        
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
        public AttributeBasedVariable MovementSlow; //Factor between 0-1, movement speed while attacking will be MovementSpeed * (1-MovementSlow)

        [Header("Attack Momentum")]
        public AttributeBasedVariable AttackMomentum;
        public float AttackMomentumDuration = 0.15f;
        public bool  LockMovementOnAttack  = false;
        public bool  LockRotationOnAttack  = false;
        [Tooltip("Seconds after movement lock before rotation is also locked. Lets the actor finish turning before freezing.")]
        public float RotationLockDelay     = 0f;

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

        public AttackPattern Clone() => (AttackPattern)MemberwiseClone();
    }

    [Serializable]
    public class ComboAttackPattern
    {
        public List<AttackPattern> Patterns = new();

        public AttackPattern GetPattern(int comboIndex)
        {
            if (Patterns == null || Patterns.Count == 0) return null;
            return Patterns[Patterns.Count > 1 ? comboIndex % Patterns.Count : 0];
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
        public ComboAttackPattern DefaultAttackPattern;
        private ComboAttackPattern _currentAttackPattern;

        [Header("Defaults")]
        public ComboAttackPatternAsset DefaultComboPatternAsset;
        public AttackPatternAsset DefaultPatternAsset;
        
        [Header("Collision")]
        public LayerMask Targets;
        public LayerMask ObstacleLayerMask;

        [Header("Combo")]
        public float ComboRefreshTime = 1f; //Time in seconds to reset the combo

        [Header("Config")] 
        [SerializeField] public float RangeTolerance = 0.1f;
        
        //Locks & Cooldowns
        //public Cooldown GlobalCooldown;
        public LockKey AttackLockKey;
    
        //Events
        public UnityAction<CombatModule> AttackStartedEvent;
        public UnityAction<CombatModule> AlignedEvent;       // fires once when rotational alignment is achieved
        public UnityAction<CombatModule> AttackedEvent;      // Deals damage here
        public UnityAction<CombatModule> AttackCompletedEvent;
        public UnityAction<Projectile> OnShotProjectileEvent; 
        public UnityAction<Actor> DamagedActorEvent;


        //Quick module references
        private LockModule _lockModule;
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

        // One-off attack pattern that bypasses the combo cycle entirely (e.g. BlockModule's bash) -- takes
        // priority in GetCurrentAttackPattern() over DefaultAttackPattern/SetCurrentAttackPattern, and never
        // advances or resets _currentComboIndex (see the guard in ExecuteAttack), so the weapon's normal
        // combo chain is exactly where it left off once the override clears. Set/cleared by whoever wants a
        // one-off action -- this module doesn't know or care who or why.
        private AttackPattern _attackPatternOverride;

        #region Lifecycle
        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            
            _statModule = Actor.GetModule<StatsModule>();
            _animationModule = Actor.GetModule<AnimationModule>();
            _slotsHandler = Actor.GetModule<ActorSlotsHandler>();
            _healthcareModule = Actor.GetModule<HealthcareModule>();
            _spellBook = Actor.GetModule<SpellBook>();
            _lockModule = Actor.GetModule<LockModule>();
            if(_lockModule != null) _lockModule.OnLocked += OnLockHandler;
        }

        public override void ModuleUpdate(float deltaTime)
        {
            if (!IsAttacking()) return;
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
                {
                    _hasAligned = HasAlignedWithAttackDirection();
                    if (_hasAligned) AlignedEvent?.Invoke(this);
                }

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

        public override void OnActorStateChanged(ActorState oldState, ActorState newState)
        {
            base.OnActorStateChanged(oldState, newState);
            if (newState == ActorState.Spawned)
                ApplyDefaultPatternAssets();
        }

        private void ApplyDefaultPatternAssets()
        {
            if (DefaultComboPatternAsset != null)
                DefaultAttackPattern = DefaultComboPatternAsset.GetComboAttackPattern();
            else if (DefaultPatternAsset != null)
                DefaultAttackPattern = new ComboAttackPattern { Patterns = new List<AttackPattern> { DefaultPatternAsset.GetAttackPattern() } };
        }
        #endregion

        #region Target
        
        /// <summary>
        /// Returns the targeted actor
        /// </summary>
        /// <returns></returns>
        public Actor GetCurrentTarget()
        {
            if(_currentCastData == null) return null;
            return _currentCastData.Target;
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
            List<IHittable> hittables = CombatUtilities.GetHittablesInArc3D(
                GetAttackPosition(), GetAttackDirection().normalized,
                GetAttackRange(), currPattern.Angle.GetValue(_statModule),
                Targets);
            DamageActors(hittables);
        }

        private void ArcAttack2D()
        {
            AttackPattern currPattern = GetCurrentAttackPattern();
            List<IHittable> hittables = CombatUtilities.GetHittablesInArc2D(
                GetAttackPosition(), GetAttackDirection().normalized,
                GetAttackRange(), currPattern.Angle.GetValue(_statModule),
                Targets);
            DamageActors(hittables);
        }

        private void LinearAttack()
        {
            AttackPattern currPattern = GetCurrentAttackPattern();
            List<IHittable> hittables = CombatUtilities.GetHittablesInBox(
                GetAttackPosition(), GetAttackDirection().normalized,
                currPattern.Width.GetValue(_statModule), GetAttackRange(),
                Targets);
            DamageActors(hittables);
        }

        private void LinearAttack2D()
        {
            AttackPattern currPattern = GetCurrentAttackPattern();
            List<IHittable> hittables = CombatUtilities.GetHittablesInBox2D(
                GetAttackPosition(), GetAttackDirection().normalized,
                currPattern.Width.GetValue(_statModule), GetAttackRange(),
                Targets);
            DamageActors(hittables);
        }

        private void CircleAttack()
        {
            List<IHittable> hittables = CombatUtilities.GetHittablesInSphere(
                GetAttackPosition(), GetAttackRange(), Targets);
            DamageActors(hittables);
        }

        private void CircleAttack2D()
        {
            List<IHittable> hittables = CombatUtilities.GetHittablesInCircle2D(
                GetAttackPosition(), GetAttackRange(), Targets);
            DamageActors(hittables);
        }
        
        /// <summary>
        /// Damages the target if in range
        /// </summary>
        public void TargetAttack()
        {
            Actor currentTarget = GetCurrentTarget();
            if (currentTarget == null) return;

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
            
            List<IHittable> hittables = CombatUtilities.GetHittablesInRaycast2D(startPoint, direction, GetAttackRange(), Targets);
            DamageActors(hittables);
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
            // OnActorHitEvent now takes any IHittable (Projectile.cs widened it) but DamagedActorEvent is
            // Actor-specific -- only forward the hit through when it actually was one.
            projectile.OnActorHitEvent = hittable =>
            {
                if (hittable is Actor actor) DamagedActorEvent?.Invoke(actor);
            };

            //Is it targeted?
            if (currentTarget != null && pattern.AttackType == AttackTypes.TargetProjectile)
            {
                Vector3 targetOffset = currentTarget.GetHitPoint(Actor).GetTargetPosition() - currentTarget.transform.position;
                projectile.Shoot(Actor, null, GetAttackPosition(), GetAttackDirection(), currentTarget.transform);
                projectile.SetTargetOffset(targetOffset);
            }
            else
            {
                // Free-aim (no locked target): fire toward wherever the caster was aiming when the attack
                // STARTED, not wherever they're facing now -- this only runs once AttackImplementationTime
                // has elapsed, so without prioritizeCastDirection a player who keeps turning during the
                // windup would see the shot fly off toward their new facing instead of the original aim.
                projectile.Shoot(Actor, null, GetAttackPosition(), GetAttackDirection(prioritizeCastDirection: true), null);
            }

            OnShotProjectileEvent?.Invoke(projectile);
        }
        
        private void DamageActors(List<IHittable> hittables)
        {
            if(!IsServerInitialized) return; //Only server can
            List<Actor> hurtActors = new List<Actor>();
            foreach(var hittable in hittables)
            {
                if(ExecuteDamageHittable(hittable) && hittable is Actor actor)
                {
                    hurtActors.Add(actor);
                }
            }
            if (!IsSpawned || hurtActors.Count == 0) return;
#if NETWORKING_NGO
            NetworkObjectReference[] refs = new NetworkObjectReference[hurtActors.Count];
            for (int i = 0; i < hurtActors.Count; i++) refs[i] = hurtActors[i].GetComponent<NetworkObject>();
            ObserverDamageActors_Rpc(refs);
#elif NETWORKING_FISHNET
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
            bool hit = ExecuteDamageHittable(actor);
            if (!hit || !IsSpawned) return;
#if NETWORKING_NGO
            ObserverDamageActor_Rpc(actor.GetComponent<NetworkObject>());
#elif NETWORKING_FISHNET
            ObserverDamageActor_Rpc(actor.GetComponent<NetworkObject>());
#endif
        }

        #region Weapon Sweep

        private Kuantech.Inventory.WeaponVisual _activeWeapon;

        /// <summary>Whatever weapon visual is currently equipped -- BlockModule reads this to play the
        /// equipped item's own block effects, same as this module reads it for melee hit effects.</summary>
        public Kuantech.Inventory.WeaponVisual GetActiveWeapon() => _activeWeapon;

        /// <summary>
        /// Points the currently-equipped melee weapon's sweep hits at this module. Called from
        /// WeaponVisual.OnEquipped()/OnUnequipped() via the item's owner chain, whenever the equipped
        /// weapon changes.
        /// </summary>
        public void SetActiveWeapon(Kuantech.Inventory.WeaponVisual weapon)
        {
            if (_activeWeapon != null) _activeWeapon.HitDetected -= OnWeaponHitDetected;
            _activeWeapon = weapon;
            if (_activeWeapon != null) _activeWeapon.HitDetected += OnWeaponHitDetected;
        }

        /// <summary>
        /// Animation-event entry points (see ActorVisualAnimationListener, which sits on the same GameObject
        /// as the Animator and forwards here) — no reason for the listener to reach for the weapon itself,
        /// this module already tracks whichever one is currently equipped via SetActiveWeapon.
        /// </summary>
        public void OnMeleeSweepStart() => _activeWeapon?.BeginSweep();
        public void OnMeleeSweepEnd() => _activeWeapon?.StopSweep();

        /// <summary>
        /// Client-authoritative detection, server-authoritative damage: WeaponVisual only ever sweeps on the
        /// owning client (see WeaponVisual.Update) because it's the only peer with an accurate, low-latency
        /// view of its own swing — the server doesn't mirror every client's attack animation, so it has no
        /// reliable way to run this same capsule query itself. The owner reports what it saw; the server
        /// decides whether that report actually deals damage.
        /// </summary>
        private void OnWeaponHitDetected(IHittable hittable, Vector3 hitPoint)
        {
            // Host (server+owner) or a server-driven actor (e.g. an AI's own weapon) -- already authoritative,
            // apply directly, no round trip needed. hittable can be null here (a wall/non-hittable) -- that's
            // fine, ApplyMeleeHit still needs to run so the clang gets broadcast to everyone else.
            if (IsServerInitialized)
            {
                ApplyMeleeHit(hittable, hitPoint);
                return;
            }

            // A remote client only ever gets here for weapons it owns (WeaponVisual gates the sweep itself),
            // but stay defensive rather than trust that invariant blindly. Nothing to do locally for the
            // owner here -- WeaponVisual already played the instant local clang/hit effect the moment it
            // detected this, before this event even fired. All that's left is telling the server.
            if (!IsOwner) return;

#if NETWORKING_NGO
            NetworkObjectReference targetRef = default;
            bool hasTarget = hittable is Actor actor && TryGetNetworkReference(actor, out targetRef);
            ReportMeleeHit_Rpc(hasTarget, targetRef, hitPoint);
#endif
        }

#if NETWORKING_NGO
        private static bool TryGetNetworkReference(Actor actor, out NetworkObjectReference reference)
        {
            NetworkObject netObj = actor.GetComponent<NetworkObject>();
            if (netObj == null) { reference = default; return false; }
            reference = netObj;
            return true;
        }

        /// <summary>Owning client reporting "I hit this" (or "I hit a wall" when hasTarget is false) -- server
        /// re-resolves the target and decides for itself whether to actually apply damage. No extra
        /// validation yet (range/attack-active/duplicate checks); add those here if this needs to be
        /// hardened against a modified client.</summary>
        [Rpc(SendTo.Server)]
        private void ReportMeleeHit_Rpc(bool hasTarget, NetworkObjectReference targetRef, Vector3 hitPoint)
        {
            IHittable hittable = null;
            if (hasTarget && targetRef.TryGet(out NetworkObject targetNetObj))
                hittable = targetNetObj.GetComponent<IHittable>();

            ApplyMeleeHit(hittable, hitPoint);
        }

        /// <summary>Broadcasts the clang/hit effect to everyone except whoever swung (they already saw it
        /// instantly, locally, via WeaponVisual, with zero network round trip).</summary>
        [Rpc(SendTo.NotOwner)]
        private void NotifyMeleeHit_Rpc(bool hasTarget, NetworkObjectReference targetRef, Vector3 hitPoint)
        {
            Actor targetActor = null;
            if (hasTarget && targetRef.TryGet(out NetworkObject targetNetObj))
                targetActor = targetNetObj.GetComponent<Actor>();

            PlayMeleeHitEffect(targetActor, hitPoint);
        }

        // targetActor is currently unused beyond the null check -- kept as the natural extension point for
        // "play a different effect when the target is an Actor vs. plain geometry" later.
        private void PlayMeleeHitEffect(Actor targetActor, Vector3 hitPoint)
        {
            EffectPlayer weaponHitEffect = _activeWeapon != null ? _activeWeapon.HitEffect : null;
            weaponHitEffect?.PlayEffectAtPosition(hitPoint, Quaternion.identity);
        }
#endif

        private void ApplyMeleeHit(IHittable hittable, Vector3 hitPoint)
        {
            bool hit = hittable != null && ExecuteDamageHittable(hittable);

#if NETWORKING_NGO
            NetworkObjectReference targetRef = default;
            bool hasTarget = hittable is Actor actor && TryGetNetworkReference(actor, out targetRef);
            if (IsSpawned) NotifyMeleeHit_Rpc(hasTarget, targetRef, hitPoint);
#endif
#if NETWORKING_FISHNET
            if (hit && IsSpawned && hittable is Actor fnActor)
                ObserverDamageActor_Rpc(fnActor.GetComponent<NetworkObject>());
#endif
        }

        #endregion

        /// <summary>
        /// Self-exclusion (never hit your own attacker) and CanBeHit() (is this even eligible right now —
        /// a destroyed prop, say) are the only checks left here. Alive/dead and faction are Actor.OnHit's
        /// job now (see HealthcareModule.OnHit and Actor.OnHit), so a basic attack reaches a corpse or a
        /// destructible exactly the way it reaches a live enemy — nothing here needs to know the difference.
        /// </summary>
        private bool ExecuteDamageHittable(IHittable hittable)
        {
            if (hittable == null || !hittable.CanBeHit() || ReferenceEquals(hittable, Actor)) return false;
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

            // Status effects and the hit VFX are Actor-specific extras on top of plain damage — a
            // destructible has neither a StatusEffectHandler nor a meaningful hit point to aim the effect at.
            if (hittable is Actor actor)
            {
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
                // Melee (sweep) attacks own their hit effect via WeaponVisual.HitEffect + the melee-sweep
                // broadcast below (exact contact point, not an approximation) -- playing AttackPattern's
                // HitEffect here too would double it up. Non-melee attack types (Arc/Circle/...) have no
                // real contact point to work with, so they still use this approximate one.
                if (IsClientInitialized && !pattern.IsMelee)
                    PlayNonMeleeHitEffect(actor);
            }

            hittable.OnHit(hitInfo);
            if (hittable is Actor hitActor) DamagedActorEvent?.Invoke(hitActor);
            return true;
        }

        // Approximate hit point (torso-height-ish, aimed back toward the attacker) for attack types that
        // have no real geometric contact point (Arc/Circle/Linear/...). Extracted so both the local
        // (host/server-as-client) path in ExecuteDamageHittable and the NGO replication RPCs below can play
        // the exact same effect on remote clients that never ran ExecuteDamageHittable themselves.
        private void PlayNonMeleeHitEffect(Actor actor)
        {
            EffectPlayer hitEffect = GetCurrentAttackPattern().HitEffect;
            if (hitEffect == null) return;
            Vector3 targetHitPoint = actor.GetHitPoint(Actor).GetTargetPosition();
            Vector3 attackerPosition = Actor.transform.position;
            attackerPosition.y = targetHitPoint.y;
            hitEffect.PlayEffectAtPosition(actor.GetHitPoint(Actor).GetTargetPositionTowardsTarget(attackerPosition), Quaternion.LookRotation(GetAttackDirection()));
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
            if(GetCurrentAttackPattern() == null) return false;
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

            // An override attack (bash, ...) is a one-off outside the weapon's own combo chain -- leave
            // _currentComboIndex exactly where it was so the chain resumes correctly once the override clears.
            if (_attackPatternOverride == null)
            {
                float timeSinceLastAttack = Time.time - _lastAttackCompleteTime;
                _currentComboIndex = timeSinceLastAttack < ComboRefreshTime ? _currentComboIndex + 1 : 0;
            }

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
                if (_animationModule != null)
                {
                    _animationModule.PlayAnimationData(currPattern.AttackAnimationData, animationTime / timeMultiplier);
                }
            }
            AttackStartedEvent?.Invoke(this);
            ApplyMovementSlow();
            return true;
        }

        private void ApplyMovementSlow()
        {
            AttackPattern currPattern = GetCurrentAttackPattern();
            MovementModule mm = Actor.GetModule<MovementModule>();
            float movementSlow = currPattern.MovementSlow.GetValue(_statModule);
            float movementSpeedMultiplier = 1 - movementSlow;
            mm.SetSpeedMultiplier(movementSpeedMultiplier);
        }

        private void RemoveMovementSlow()
        {
            AttackPattern currPattern = GetCurrentAttackPattern();
            MovementModule mm = Actor.GetModule<MovementModule>();
            mm.SetSpeedMultiplier(1);
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

        public bool IsAttacking()
        {
            return _isAttacking;
        }

        public int GetCurrentComboIndex()
        {
            float timeSinceLastAttack = Time.time - _lastAttackCompleteTime;
            if (timeSinceLastAttack >= ComboRefreshTime)
            {
                return 0;
            }

            return _currentComboIndex;
        }

        /// <summary>
        /// Checks if attack pattern is melee
        /// </summary>
        /// <returns></returns>
        public bool IsMelee()
        {
            AttackPattern currPattern = GetCurrentAttackPattern();
            if (currPattern == null) return false;
            AttackTypes attackType = currPattern.AttackType;
            if (attackType == AttackTypes.RangedProjectile || attackType == AttackTypes.RangedRaycast) return false;
            return GetCurrentAttackPattern().IsMelee;
        }

        public AttackPattern GetCurrentAttackPattern()
        {
            if (_attackPatternOverride != null) return _attackPatternOverride;
            var combo = _currentAttackPattern ?? DefaultAttackPattern;
            return combo?.GetPattern(_currentComboIndex);
        }

        public void SetCurrentAttackPattern(AttackPattern attackPattern)
        {
            ComboAttackPattern cap = new ComboAttackPattern();
            cap.Patterns.Add(attackPattern);
            SetCurrentComboAttackPattern(cap);
        }
        public void SetCurrentComboAttackPattern(ComboAttackPattern combo)
        {
            _currentAttackPattern = combo;
        }

        /// <summary>
        /// Set by whoever needs a one-off attack outside the normal combo cycle (e.g. BlockModule while
        /// blocking) -- see the field doc on _attackPatternOverride. Pass null to clear it and fall back to
        /// the regular combo/default pattern again.
        /// </summary>
        public void SetAttackPatternOverride(AttackPattern pattern)
        {
            _attackPatternOverride = pattern;
        }
        #endregion

        #region Attack Checks

        /// <summary>
        /// Checks if actor is in a state that can attack. Doesn't check for any range etc.
        /// </summary>
        /// <returns></returns>
        public bool CanAttack()
        {
            return Actor.IsAlive() && !IsAttackInCooldown() && !IsAttackLocked() && HasResourcesToAttack();
        }

        public bool HasResourcesToAttack()
        {
            AttackPattern attackPattern = GetCurrentAttackPattern();
            if (_healthcareModule != null && attackPattern.RequiredResource != null)
            {
                float currentResource = _healthcareModule.GetCurrentResource(attackPattern.RequiredResource);
                if (currentResource < attackPattern.RequiredResourceAmount) return false;
            }
            return true;
        }

        /// <summary>
        /// Checks if the action cast data can be checked
        /// </summary>
        /// <param name="actionCastData"></param>
        /// <returns></returns>
        public bool CanCastAction(ActionCastData actionCastData)
        {
            AttackPattern attackPattern = GetCurrentAttackPattern();

            //Check target
            if (attackPattern.AttackType == AttackTypes.Target)
            {
                if(actionCastData.Target == null) return false;

                if(!IsInAttackRange(actionCastData.Target.GetHitPoint(Actor)))
                {
                    return false;
                }
            } 
            return true;
        }

        public bool IsAttackInCooldown()
        {
            if (IsAttacking()) return true;
            return false;
        }

        public bool IsAttackLocked()
        {
            if(_lockModule == null) return false;
            return _lockModule.IsLocked(AttackLockKey);
        }

        public bool IsInAttackRange(Actor actor)
        {
            return IsInAttackRange(actor.GetHitPoint(Actor));
        }

        /// <summary>
        /// Checks whether the target point is in attacking range
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool IsInAttackRange(WorldPoint target)
        {
            float dist = Vector3.Magnitude(target.GetTargetPosition() - Actor.GetActorLocation()) - target.Radius - Actor.ActorRadius;
            return dist <= (GetAttackRange() + RangeTolerance);
        }
        #endregion

        #region Locking
        public void LockAttack(object locker)
        {
            if (_lockModule == null) return;
            _lockModule.Lock(AttackLockKey, locker);
        }

        public void UnlockAttack(object locker)
        {
            if (_lockModule == null) return;
            _lockModule.Unlock(AttackLockKey, locker);
        }
        
        private void OnLockHandler(string lockKey)
        {
            if(lockKey == AttackLockKey.LockId)
            {
                //Cancel attack
                EndAttack();
            }
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
            if (!_isAttacking) return;
            _isAttacking = false;
            _lastAttackCompleteTime = Time.time;
            RemoveMovementSlow(); //TODO: this is probably will be a runtime bug. We can't just set speed multiplier to 1 like this
            // Safety net: if the attack got cut short (interrupted/staggered) before the animation's
            // OnSweepEnd event fired, this stops the sweep from running forever.
            OnMeleeSweepEnd();
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
            playSettings.EffectSpeedMultiplier = GetAttackSpeedMultiplier(); // effect scales with attack speed
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
        
        public Vector3 GetAttackDirection(bool prioritizeCastDirection = false)
        {
            Actor target = GetCurrentTarget();
            Vector3 direction = Vector3.zero;
            if (target != null)
            {
                direction = target.transform.position - GetActionCastData().StartPosition;
                if(!prioritizeCastDirection)
                {
                    return direction.normalized;
                }
            }

            ActionCastData castData =  GetActionCastData();
            if (castData.Direction.sqrMagnitude > 0.01f && prioritizeCastDirection)
            {
                return castData.Direction;
            }

            if(direction.sqrMagnitude > 0.01f) return direction; //If direciton is not zero...

            // No explicit direction or target (e.g. a free-aim melee): use the actor's CURRENT facing so the
            // attack points where the avatar is looking at IMPLEMENTATION time. Turning mid-swing then aims the
            // hit where you end up, instead of a direction frozen at the press. (Swap to
            // MotionVectorsHandler.GetTargetVector() if you'd rather it lead toward the raw aim ahead of the turn.)
            Vector3 facing = Actor.transform.forward;
            if (facing.sqrMagnitude > 0.01f) return facing.normalized;

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
        // Dead code -- NETWORKING_FISHNET is never defined, kept only as a reference for what this looked
        // like before the NGO port below. ExecuteDamageActor (called here) was never actually defined
        // anywhere in this file even when this block was live; don't resurrect this path.
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
#elif NETWORKING_NGO
        // Client -> server: attacking client already ran ExecuteAttack() locally (optimistic, zero-lag
        // local feedback) before this ever gets sent -- see Attack(). This is what makes the SERVER's copy
        // of a remote client's actor actually start attacking too.
        [Rpc(SendTo.Server)]
        private void ServerAttack_Rpc(ActionCastData castData)
        {
            Attack(castData);
        }

        [Rpc(SendTo.Server)]
        private void ServerCancelAttack_Rpc()
        {
            ExecuteEndAttack();
            ObserverAttackEnd_Rpc();
        }

        // Server -> everyone but the owner (owner already ran ExecuteAttack locally; the server ALSO
        // already ran it directly inside Attack(), so it skips here too).
        [Rpc(SendTo.NotOwner)]
        private void ObserverAttackStart_Rpc(ActionCastData castData)
        {
            if (IsServerInitialized) return;
            ExecuteAttack(castData);
        }

        // Everyone, INCLUDING the owner -- unlike attack-start, nobody runs RunAttackImplementation/
        // ExecuteEndAttack optimistically on their own; ModuleUpdate only ever decides this on the server
        // (see the IsServerInitialized gate around both triggers there), so the owner's own _isAttacking/
        // combo state depends entirely on this RPC actually reaching them. Excluding the owner here was
        // the original bug: it left a remote client's _isAttacking stuck true forever after one swing.
        [Rpc(SendTo.Everyone)]
        private void ObserverAttackImplementation_Rpc()
        {
            if (IsServerInitialized) return;
            RunAttackImplementation();
        }

        [Rpc(SendTo.Everyone)]
        private void ObserverAttackEnd_Rpc()
        {
            if (IsServerInitialized) return;
            ExecuteEndAttack();
        }

        // Damage itself is never re-applied on a client -- HealthcareModule's own replication already
        // carries the actual health change. This only replays the cosmetic hit effect for whoever didn't
        // run ExecuteDamageHittable themselves -- which, for non-melee attack types, is EVERYONE including
        // the attacker (DamageActor/DamageActors both early-out unless IsServerInitialized, so a remote
        // attacking client never ran it either).
        [Rpc(SendTo.Everyone)]
        private void ObserverDamageActor_Rpc(NetworkObjectReference targetRef)
        {
            if (IsServerInitialized) return;
            if (targetRef.TryGet(out NetworkObject targetNetObj) && targetNetObj.TryGetComponent(out Actor actor))
                PlayNonMeleeHitEffect(actor);
        }

        [Rpc(SendTo.Everyone)]
        private void ObserverDamageActors_Rpc(NetworkObjectReference[] targets)
        {
            if (IsServerInitialized) return;
            foreach (var targetRef in targets)
            {
                if (targetRef.TryGet(out NetworkObject targetNetObj) && targetNetObj.TryGetComponent(out Actor actor))
                    PlayNonMeleeHitEffect(actor);
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


