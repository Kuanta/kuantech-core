using System;
using System.Collections.Generic;
using Kuantech.Combat;
using Kuantech.Core.Inventory;
using Kuantech.Core.Rpg;
using Kuantech.Data;
using Kuantech.Inventory;
using Kuantech.Inventory.Items;
using Kuantech.Core.UI;
using Kuantech.Core.Utils;
using Kuantech.Utils;
using Sirenix.OdinInspector;
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
        Ranged, //For projecitle based attacks, like arrow and fireball
        RangedRaycast, //For raycast based attacks
        Target,
        TargetProjectile,
    }
    
    //Event Infos
    public struct ProjecitleShotInfo
    {
        public Vector3 From;
        public Vector3 Direction;
        public Projectile Projectile;
    }

    public struct ProjectileImpactInfo
    {
        public Actor Impacted;
        public Projectile Projectile;
    }

    public struct AttackStartData
    {
        public int flowIndex;
        public bool IsAlternativeAttack;
        public bool PlayEffect;
    }
    public class CombatModule : Module
    {
        //Components
        public LineTelemetry LineAttackTelemetry;
        public CircleTelemetry CircleAttackTelemetry;
        public ArcTelemetry ArcAttackTelemetry;
        
        //Base Parameters for Unarmed Combat (Or for enemies that doesn't need 'equipment'
        public WeaponAttackPattern DefaultAttackPattern;
        public WeaponAttackPattern CurrentAttackPattern;
        public AttackTypes CurrentAttackType;
        public Weapon EquippedWeapon = null;
        public Projectile DefaultProjectilePrefab = null;

        [Header("Attack Positions")] 
        public Vector3 AimVector;
        public Transform ShootPosition;
        
        //Targets
        public Actor CurrentTarget;
        
        //States
        public float AttackStartTime;
        public bool IsAttacking = false;
        private bool _damageDone = false; //Checks if DamageTime has passed
        private Telemetry ActiveTelemetry;
        
        //Locks & Cooldowns
        public Cooldown GlobalCooldown;
        public LockVariable AttackLock = new LockVariable();
        public LockVariable SkillLock = new LockVariable();
        
        //Collision
        private Collider[] _results = new Collider[32];
        public LayerMask Targets;
        public LayerMask ObstacleLayerMask;
        private UnityAction AttackCompleteHandler;
        private InventoryModule _actorInventory;
        private List<RaycastProjectile> _shotRaycastProjecitles = new List<RaycastProjectile>();

        //Attack Flow (Combo)
        [Header("Flow")]
        [SerializeField] private int _flowIndex = 0;
        [SerializeField] private float _flowBreakTime = 0.5f;
        [SerializeField] private float QueueTimeFactor = 1.1f;
        private float _lastAttackEndTime = 0f;
        private bool _attackQueued = false;
        
        //AttackSkill
        [SerializeReference] private Dictionary<Type, Skill> Skills = new Dictionary<Type, Skill>();
        public float ManaCost = 0;
        public bool CanUseSkill = false;
        public bool IsResistantToKnockback = false;
        
        //Events
        public EventHandler<AttackStartData> AttackStartEvent;
        public EventHandler<int> AttackEvent;
        public EventHandler<Actor> MeleeImpactEvent;
        public EventHandler<ProjectileImpactInfo> RangedImpactEvent;
        public EventHandler<Projectile> ProjectileEndRangeEvent;
        public EventHandler<ProjecitleShotInfo> ProjectileShotEvent;
        public EventHandler<RaycastHit> RayProjectileHitEvent; 
        
        //Getters
        public float GetMovementFactor()
        {
            return IsAttacking ?
                //Use 1 - slow since default value for floats are 0. This way we are ensuring that non initialized slow factor values wont result in 0 movements
                Mathf.Clamp(1 - CurrentAttackPattern.MovementSlow, 0f, 1f) : 1f;
        }
        
        /// <summary>
        /// Checks whether the actor can attack or not
        /// </summary>
        /// <returns></returns>
        public bool CanAttack()
        {
            if (CurrentAttackPattern.AttackType == AttackTypes.Target && CurrentTarget == null) return false;
            return !IsAttacking && GlobalCooldown.IsOffCooldown() && !AttackLock.IsLocked() && Actor.Health > 0 ;
        }

        public bool CanCastSkill()
        {
            return GlobalCooldown.IsOffCooldown() && !SkillLock.IsLocked() && Actor.Health > 0;
        }
        
        public override void Initialize()
        {
            base.Initialize();
            _actorInventory = Actor.GetComponent<InventoryModule>();
            
            //Remove all subscriptions
            AttackEvent = null;
            MeleeImpactEvent = null;
            RangedImpactEvent = null;
            ProjectileEndRangeEvent = null;
            ProjectileShotEvent = null;
            ApplyAttackPattern(DefaultAttackPattern);
            GlobalCooldown = new Cooldown(1f);
        }
        
        public override void Reset() 
        {
            //Locks
            AttackLock.Reset();
            RangedAttackLock.Reset();
            SkillLock.Reset();
            
            IsAttacking = false;
            AttackStartTime = 0;
            _flowIndex = 0;
            _damageDone = false;
            _attackQueued = false;
            _shotRaycastProjecitles.Clear();
            ResetActiveTelemetry();
            CalculateManaCosts();
        }

        private void ResetActiveTelemetry()
        {
            if (ActiveTelemetry == null) return;
            ActiveTelemetry.gameObject.SetActive(false);
            ActiveTelemetry.SetFill(0f);
        }
        private void Update()
        {
            if (GameManager.Instance.GameIsPaused) return;
            
            //1) Handle projectile bullets
            HandleProjectileBullets();
            
            //2) Attack Anim
            if (!IsAttacking) return;
            float timePassedAttacking = Time.time - AttackStartTime;
            
            //Check animation time first
            if (timePassedAttacking >= CurrentAttackPattern.AnimationTime)
            {
                IsAttacking = false; //Ends the attack
                _lastAttackEndTime = Time.time;
                if (!_attackQueued) return;
                _attackQueued = false;
                Attack();
                return;
            }

            if (_damageDone) return;
            float normalizedTime = timePassedAttacking / CurrentAttackPattern.DamageTime;
            
            if(ActiveTelemetry != null) ActiveTelemetry.SetFill(normalizedTime);
            if (normalizedTime > 1f)
            {
                ResetActiveTelemetry();
                AttackEvent?.Invoke(this, _flowIndex);
                switch (CurrentAttackType)
                {
                    case AttackTypes.Arc:
                        ArcMeleeAttack();
                        break;
                    case AttackTypes.Linear:
                        LinearMeleeAttack();
                        break;
                    case AttackTypes.Circle:
                        CircleMeleeAttack();
                        break;
                    case AttackTypes.Ranged:
                        RangedAttack();
                        break;
                    case AttackTypes.RangedRaycast:
                        RangedRaycastAttack();
                        break;
                    case AttackTypes.Target:
                        TargetAttack();
                        break;
                }
                AttackCompleteHandler?.Invoke();
                _damageDone = true;
            }
            

        }

        private void HandleProjectileBullets()
        {
            _shotRaycastProjecitles.RemoveAll(proj => proj.Impacted == true);
            foreach (var proj in _shotRaycastProjecitles)
            {
                proj.Update(Time.deltaTime);
            }
        }
        
        public void ApplyAttackPattern(WeaponAttackPattern pattern)
        {
            //First parameters...
            CurrentAttackPattern = pattern;
            
            //Apply bonuses
            CurrentAttackPattern.Range += Actor.Stats.GetStat(StatTypes.RangeBonus);

            float attackSpeedBonus = Actor.Stats.GetStat(StatTypes.AttackSpeedBonus);
            attackSpeedBonus = Mathf.Max(attackSpeedBonus, 1);
            CurrentAttackPattern.DamageTime /= attackSpeedBonus;
            CurrentAttackPattern.AnimationTime /= attackSpeedBonus;
            
            //...then attack type
            SetAttackType(CurrentAttackPattern.AttackType);
            SetAnimationTime(); //For clients
        }

        public bool AlternativeAttack()
        {
            if (!CanAttack()) return false;
            if (EquippedWeapon != null)
            {
                ApplyAttackPattern(EquippedWeapon.AlternativeAttackPattern);
                CurrentAttackPattern.Damage = EquippedWeapon.GetAlternativeDamage();
            }
            if(!_Attack(new AttackStartData
               {
                   flowIndex = _flowIndex,
                   IsAlternativeAttack = true,
                   PlayEffect = true
               },false, null)) return false; //Attack pattern is applied on previous line
            PlayAttackAnimation(true);
            return true;
        }
        
        public bool Attack(bool applyAttackPattern = true, UnityAction attackCompleteHandler = null)
        {
            if (!CanAttack()) return false;
            if(!_Attack(new AttackStartData
               {
                   flowIndex = _flowIndex,
                   IsAlternativeAttack = false,
                   PlayEffect = true
               },applyAttackPattern, attackCompleteHandler)) return false;
            PlayAttackAnimation(false);
            return true;
        }
        
        public void PlayAttackAnimation(bool alternativeAnimationSet = false)
        {
            PlayAttackAnimation(GetFlowIndex(), alternativeAnimationSet);
        }

        public void PlayAttackAnimation(int flowIndex, bool alternativeAnimationSet = false)
        {
            AnimatorModule animMod = (AnimatorModule)Actor.GetModuleByType(typeof(AnimatorModule));

            if (alternativeAnimationSet)
            {
                if (animMod != null)
                {
                    animMod.AlternativeAttackTrigger(attackIndex:flowIndex, handIndex:0);
                }

                return;
            }
            if (animMod != null)
            {
                animMod.LightAttackTrigger(attackIndex:flowIndex, handIndex:0); //todo: Consider supporting off hand weapons
            }
        }
        /// <summary>
        /// Handles the damaging part of the attack process. Doesn't check for global cooldown or attack lock.
        /// Can be used for "forced" cases
        /// </summary>
        /// <param name="attackCompleteHandler"></param>
        public bool _Attack(AttackStartData attackStartData, bool applyAttackPattern = true, UnityAction attackCompleteHandler = null)
        {
            //Check weapon
            if (_actorInventory != null )
            {
                //Don't safety check the type. It should be weapn
                Item item = _actorInventory.equipment.GetEquipedItem(Enums.EquipmentSlotType.MainHand);
                if (item != null)
                {
                    EquippedWeapon = (Weapon) item;
                }
            }

            if (EquippedWeapon != null)
            {
                if (Time.time - _lastAttackEndTime <= _flowBreakTime)
                {
                    _flowIndex++;
                }
                else
                {
                    _flowIndex = 0;
                }
                if (_flowIndex >= EquippedWeapon.AttackPatterns.Count) _flowIndex = 0;
                if (applyAttackPattern)
                {
                    WeaponAttackPattern attackPattern = EquippedWeapon.AttackPatterns[_flowIndex];
                    attackPattern.Damage = EquippedWeapon.GetDamage(_flowIndex);
                    ApplyAttackPattern(attackPattern);
                }
            }
            else if(applyAttackPattern)
            {
                ApplyAttackPattern(DefaultAttackPattern);
            }
            
            AttackCompleteHandler = attackCompleteHandler;
            IsAttacking = true;
            _damageDone = false;
            AttackStartTime = Time.time;
            if(ActiveTelemetry != null) ActiveTelemetry.gameObject.SetActive(true);
            
            //Check mana costs
            CanUseSkill = false;
            if (Actor.Energy - ManaCost >= 0)
            {
                //Use kills
                Actor.SpendEnergy(ManaCost);
                CanUseSkill = true;
            }
             
            AttackStartEvent?.Invoke(this, attackStartData);
            return true;
        }
        
        /// <summary>
        /// Updates attack pattern. Useful for cases where player receives a range buff 
        /// </summary>
        public void UpdateAttackPattern()
        {
            EquippedWeapon = Actor.InventoryModule.equipment.GetEquipedItem(Enums.EquipmentSlotType.MainHand) as Weapon;
            ApplyAttackPattern(EquippedWeapon != null ? EquippedWeapon.AttackPatterns[_flowIndex] : DefaultAttackPattern);
        }
        
        public void SetAimVector(Vector3 aimVector)
        {
            AimVector = aimVector;
        }

        private int GetFlowIndex()
        {
            if (Time.time - _lastAttackEndTime <= _flowBreakTime)
            {
                return _flowIndex;
            }
            else
            {
                return 0;
            }
        }
        public void Cancel()
        {
            IsAttacking = false;
            _attackQueued = false;
            ResetActiveTelemetry();
        }
        public void SetAttackType(AttackTypes attackType)
        {
            //todo: handle telemetry for ranged
            CurrentAttackType = attackType;
            switch (attackType)
            {
                case AttackTypes.None:
                    ActiveTelemetry = null;
                    break;
                case AttackTypes.Linear:
                    ActiveTelemetry = LineAttackTelemetry;
                    break;
                case AttackTypes.Arc:
                    ActiveTelemetry = ArcAttackTelemetry;
                    break;
                case AttackTypes.Circle:
                    ActiveTelemetry = CircleAttackTelemetry;
                    break;
                default:
                    ActiveTelemetry = null;
                    break;
            }

            if (ActiveTelemetry != null)
            {
                ActiveTelemetry.SetAngle(CurrentAttackPattern.Angle);
                ActiveTelemetry.SetLength(CurrentAttackPattern.Range);
                ActiveTelemetry.SetWidth(CurrentAttackPattern.Width);
                ResetActiveTelemetry();
            }
        }
        
        /// <summary>
        /// Checks whether a target is in the attack range. Current attack pattern will be used
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool IsInAttackRange(Transform target)
        {
            Vector3 diffVector = target.position - transform.position;
            diffVector.y = 0;
            float sqrDist = diffVector.sqrMagnitude;
            Vector3 relativeForward = transform.GetRelativeForwardVector(target);
            Vector3 relativeRight = transform.GetRelativeRightVector(target);

            float forwardDistrSqr = relativeForward.sqrMagnitude;
            float widthSqr = relativeRight.sqrMagnitude;

            switch (CurrentAttackType)
            {
                case AttackTypes.Linear:
                    return forwardDistrSqr <= CurrentAttackPattern.Range * CurrentAttackPattern.Range && widthSqr <= 
                        (CurrentAttackPattern.Width * CurrentAttackPattern.Width * 0.5f * 0.5f) && Vector3.Dot(transform.forward, diffVector) >= 0;
                case AttackTypes.Arc:
                    bool pointInAngle = transform.PointIsInAngleRange(target, CurrentAttackPattern.Angle);
                    bool isInRange = sqrDist <= CurrentAttackPattern.Range * CurrentAttackPattern.Range;
                    return pointInAngle && isInRange;
                case AttackTypes.RangedRaycast:
                case AttackTypes.Ranged:
                case AttackTypes.Circle:
                    return sqrDist <= CurrentAttackPattern.Range * CurrentAttackPattern.Range;
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Simply checks if target is inside a circular target
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool IsInCircularRange(Transform target)
        {
            Vector3 diffVector = target.position - transform.position;
            diffVector.y = 0;
            float sqrDist = diffVector.sqrMagnitude;
            return sqrDist <= CurrentAttackPattern.Range * CurrentAttackPattern.Range;
        }
        public float GetDamage()
        {
            float damageBonus = Actor.Stats.GetStat(StatTypes.DamageBonus);
            return GetBaseDamage() + damageBonus;
        }

        public float GetBaseDamage()
        {
            return Mathf.Max(CurrentAttackPattern.Damage, 1f);
        }
        
        public Vector3 GetShootPosition()
        {
            return ShootPosition != null ? ShootPosition.position : transform.position;
        }
        #region Attack Casts
        //Attack Casts
        public static bool CheckObstaclesBetween(GameObject source, GameObject destionation, float range, LayerMask obstacleLayerMask, float heightOffset = 1f)
        {
            Vector3 center = source.transform.position + Vector3.up * heightOffset;
            //Check if behind obstacle
            Vector3 diffVec = destionation.transform.position - center;
            diffVec.Normalize();

            Ray ray = new Ray
            {
                origin = center,
                direction = diffVec
            };
            bool obscured = false;
            RaycastHit[] hits = new RaycastHit[16];

            if (UnityEngine.Physics.RaycastNonAlloc(ray, hits, range, obstacleLayerMask.value) > 0)
            {
                for (int j = 0; j < hits.Length; ++j)
                {
                    if (hits[j].collider.gameObject == destionation)
                    {
                        break;
                    }

                    if (hits[j].collider.gameObject == source)
                    {
                        continue;
                    }

                    obscured = true;
                    break;
                }
            }
            return false;
        }
        public List<Actor> GetCircularAreaEnemies(float range, bool checkObstacle, float angle = 0f)
        {
            List<Actor> attackables = new List<Actor>();
            Vector3 center = transform.position + Vector3.up;
            Collider[] results = new Collider[32];
            int hitCount = UnityEngine.Physics.OverlapSphereNonAlloc(center, range, results, Targets);
             Vector3 forwardVector = transform.forward;
            forwardVector.y = 0f;
            for (int i = 0; i < hitCount; ++i)
            {
                if (results[i] == null) continue;
                Actor target = results[i].GetComponent<Actor>();
                if (target == null || target.FactionId == this.Actor.FactionId || target == Actor) continue; //todo: Implement a better faction checking (consider neutrals)
                
                bool isInAngleRange = false;
                Collider collider = results[i];
                var bounds = collider.bounds;
                List<Vector3> colliderCorners = new List<Vector3>
                {
                    new Vector3(bounds.center.x, 0, bounds.center.z),
                    new Vector3(bounds.center.x + bounds.extents.x, 0, bounds.center.z + bounds.extents.z),
                    new Vector3(bounds.center.x + bounds.extents.x, 0, bounds.center.z - bounds.extents.z),
                    new Vector3(bounds.center.x - bounds.extents.x, 0, bounds.center.z + bounds.extents.z),
                    new Vector3(bounds.center.x - bounds.extents.x, 0, bounds.center.z - bounds.extents.z),
                };

                //At least the object's center should be in front
                if (Vector3.Angle(forwardVector, colliderCorners[0] - transform.position) > 90f) continue;

                float _minAngle = 0f;
                float _maxAngle = 0f;
                bool _minFlag = false;
                bool _maxFlag = false;
                float halfAngle = angle * 0.5f + 1f;
                foreach (var corner in colliderCorners)
                {
                    //Check angle
                    Vector3 differenceVector = corner - transform.position;
                    differenceVector.y = 0;
                    float angleOfCollider = Vector3.SignedAngle(differenceVector, forwardVector, Vector3.up);
                    if (angleOfCollider >= -90f && angleOfCollider < _minAngle)
                    {
                        _minAngle = angleOfCollider;
                        _minFlag = true;
                    }

                    if (angleOfCollider <= 90f && angleOfCollider > _maxAngle)
                    {
                        _maxAngle = angleOfCollider;
                        _maxFlag = true;
                    }

                    if (Mathf.Abs(angleOfCollider) <= halfAngle) isInAngleRange = true; //1f for safety check
                }

                //We still need to check if all angles were outside (if collider is too big and all angles are outside the range
                if (_maxFlag && _minFlag && _minAngle <= -halfAngle && _maxAngle >= halfAngle) isInAngleRange = true;

                if (!isInAngleRange) continue;

                if (checkObstacle)
                {
                    bool obscured = CheckObstaclesBetween(gameObject, results[i].gameObject, range, ObstacleLayerMask);
                    if (obscured) continue;
                }
            
           
                attackables.Add(target);
            }
            return attackables;
        }
            
        public static void DealCircularAreaDamage(CombatModule from, float damage, float range, float knockback, float knockbackTime, bool useSkill, bool checkObstacle, float angle=0f)
        {
            List<Actor> attackables =
                from.GetCircularAreaEnemies(range, checkObstacle, angle);
            foreach (var actor in attackables)
            {
                if(actor == null) continue;
                from.DamageActorMelee(actor, damage, knockback, knockbackTime);   
            }
        }

        public List<Actor> GetLinearAreaEnemies(float range, float width, bool checkObstacle)
        {
            List<Actor> attackables = new List<Actor>();
            Vector3 center = transform.position + transform.forward * range*0.5f + Vector3.up;
            Vector3 rayOrigin = transform.position + Vector3.up;
            center.y = 1f;
            Collider[] results = new Collider[32];
            int hitCount = UnityEngine.Physics.OverlapBoxNonAlloc(center, 
                new Vector3(width*0.5f,
                    width*0.5f, 
                    range*0.5f), 
                results, Quaternion.identity, Targets.value);
            
            for (int i = 0; i < hitCount; ++i)
            {
                if(results[i] == null) continue;
                Actor target = results[i].GetComponent<Actor>();
                if(target == null || target == Actor) continue;

                if (checkObstacle)
                {
                    bool obscured = CheckObstaclesBetween(gameObject, results[i].gameObject, range, ObstacleLayerMask);
                    if (obscured) continue;
                }
                attackables.Add(target);
            }
            return attackables;
        }
        public static void DealLinearAreaDamage(CombatModule from, float damage, float range, float width, float knockback, float knockbackTime, bool checkObstacle)
        {
            List<Actor> attackables = from.GetLinearAreaEnemies(range, width, checkObstacle);
            foreach (var target in attackables)
            {
                from.DamageActorMelee(target,damage, knockback, knockbackTime);
            }
        }
        
        private void LinearMeleeAttack()
        {
            DealLinearAreaDamage(this, GetDamage(), CurrentAttackPattern.Range, CurrentAttackPattern.Width, CurrentAttackPattern.Knockback, CurrentAttackPattern.KnockbackTime, true);
        }
        private void ArcMeleeAttack()
        {
            ArcMeleeAttack(CurrentAttackPattern.Angle);
        }

        private void ArcMeleeAttack(float angle)
        {
            DealCircularAreaDamage(this, GetDamage(), CurrentAttackPattern.Range, CurrentAttackPattern.Knockback, CurrentAttackPattern.KnockbackTime, true, true, CurrentAttackPattern.Angle);
        }

        private void CircleMeleeAttack()
        {
            ArcMeleeAttack(360f);
        }
        
        public LockVariable RangedAttackLock = new LockVariable();

        private void RangedAttack()
        {
            if (RangedAttackLock.IsLocked()) return;
            GameObject projectilePrefab = null;
            // if (EquippedWeapon != null)
            // {
            //     projectilePrefab = EquippedWeapon.ProjectilePrefab;
            // }
            // if (DefaultProjectilePrefab != null)
            // {
            //     projectilePrefab = DefaultProjectilePrefab.gameObject;
            // }
            projectilePrefab = CurrentAttackPattern.ProjectilePrefab;
            if (projectilePrefab == null)
            {
                Debug.LogError("Tried to shoot projectile with no default projectile and null equipped weapon");
                return;
            }

            Actor target = CurrentAttackPattern.TargetedProjectile ? CurrentTarget : null;
            
            ShootProjectile(EquippedWeapon, projectilePrefab, 
                GetShootPosition(), 
                transform.forward,
                true,target.transform,
                CurrentAttackPattern.ProjectileRisHeight);
        }
        
        /// <summary>
        /// Shoots a raycast projectile
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="direction"></param>
        /// <param name="maxRange"></param>
        public void RangedRaycastAttack()
        {
            if (RangedAttackLock.IsLocked()) return;
            Vector3 origin = GetShootPosition();
            RaycastProjectile proj = new RaycastProjectile();
            _shotRaycastProjecitles.Add(proj);
            proj.Shoot(origin, AimVector, CurrentAttackPattern.ProjectileSpeed,  CurrentAttackPattern.Range, OnRaycastProjectileHit, CurrentAttackPattern.ProjectileDrop);
        }

        public void TargetAttack()
        {
            float damage = GetDamage();
            Actor target = CurrentTarget;
            if (target == null || target.Health <= 0f) return;
            target.ReceiveDamage(Actor, damage);
            //todo: Discuss knockback
        }
        private void OnRaycastProjectileHit(RaycastHit hitInfo)
        {
            RayProjectileHitEvent?.Invoke(this, hitInfo);
            Actor actor = hitInfo.collider.gameObject.GetComponent<Actor>();
            if (actor == null) return;
            actor.ReceiveDamage(Actor, GetDamage());
        }

        public Projectile ShootProjectile(Weapon weapon, GameObject projectilePrefab, bool castSkill,Transform target = null, 
            float riseHeight = 0f)
        {
            Vector3 shootPosition = GetShootPosition();
            Vector3 direction = transform.forward;
            return ShootProjectile(weapon, projectilePrefab, shootPosition, direction, castSkill, target, riseHeight);
        }
        
        /// <summary>
        /// Shoots a projectile from the weapon
        /// </summary>
        /// <param name="weapon"></param>
        /// <param name="projectilePrefab"></param>
        /// <param name="direction"></param>
        /// <param name="castSkill"></param>
        /// <param name="shootPosition"></param>
        public Projectile ShootProjectile(Weapon weapon, GameObject projectilePrefab, Vector3 shootPosition, Vector3 direction, bool castSkill,
            Transform target = null, 
            float riseHeight = 0f)
        {
            if (projectilePrefab == null) return null;
            PrefabPool pool = GameManager.Instance.Pool;
            GameObject projectileObj = pool.GetObject(projectilePrefab);
            Quaternion shootRotation = Quaternion.LookRotation(direction);
            if (projectileObj.TryGetComponent(out Projectile projectile))
            {
                projectile.Initialize(this, weapon, shootPosition, shootRotation, target, riseHeight);
 
                if (CanUseSkill && castSkill)
                {
                    ProjectileShotEvent?.Invoke(this, new ProjecitleShotInfo()
                    {
                        Projectile = projectile,
                        Direction = direction,
                        From = shootPosition,
                    });
                }
                projectileObj.SetActive(true);

                return projectile;

            }
            pool.PoolObject(projectileObj);
            return null;
        }

        public Throwable ShootThrowable(Weapon weapon, GameObject throwablePrefab,  Vector3 shootPosition, Vector3 direction, float horizontalDistance, float horizontalSpeed, float initialHeight)
        {
            if (throwablePrefab == null) return null;
            PrefabPool pool = GameManager.Instance.Pool;
            GameObject throwableObj = pool.GetObject(throwablePrefab);
            if (throwableObj.TryGetComponent(out Throwable throwable))
            {
                throwable.Throw(this, weapon, shootPosition, Quaternion.LookRotation(direction), horizontalDistance, horizontalSpeed, direction.Get2D(), -9.8f, initialHeight);
                throwable.transform.position = shootPosition;
                throwable.transform.rotation = Quaternion.LookRotation(direction);
                return throwable;
            }
            pool.PoolObject(throwableObj);
            return null;
        }
        
        /// <summary>
        /// Calculates the projectile damage to given actor
        /// </summary>
        /// <param name="projectile"></param>
        /// <param name="target"></param>
        public void OnProjectileImpact(Projectile projectile, Actor target)
        {
            if (CanUseSkill)
            {
                RangedImpactEvent?.Invoke(this, new ProjectileImpactInfo
                {
                    Projectile = projectile,
                    Impacted = target,
                });
            }
            target.ReceiveDamage(Actor, projectile.Damage);
        }
        #endregion

        public void DamageActorMelee(Actor target, float damage, float knockback, float knockbackTime)
        {
            if (target.Health <= 0f || Actor.FactionId == target.FactionId) return;
            Debug.LogError($"{gameObject.name} attacked {target.name}");
            target.ReceiveDamage(Actor, damage);
            
            KnockbackActor(this, target, transform.forward, knockback, knockbackTime);
            MeleeImpactEvent?.Invoke(this, target);

        }

        /// <summary>
        /// Applies a knockback to target
        /// </summary>
        /// <param name="target">Actor to apply knockback</param>
        /// <param name="knockback">Amount of knockback force</param>
        /// <param name="knockbackTime">Duration of knockback</param>
        public static void KnockbackActor(CombatModule applier, Actor target, Vector3 direction, float knockback, float knockbackTime)
        {
            if (knockback == 0) return;
            //todo: calculate knockback from main stat
            if (target.CombatModule.IsResistantToKnockback) return; //todo: Move knockback resistance to actor 
            float momentum = 0f;
            if (applier != null && applier != null)
            {
                momentum = applier.GetMomentum(target);
            }
            target.MovementModule.Knockback(direction, knockback+momentum, knockbackTime);
        }

        public float GetMomentum(Actor target)
        {
            //Calculate momentum 
            float momentum = 0;

            if (Actor.Rigidbody == null) return momentum;
            Vector3 velocity = Actor.MovementModule.GetMomentumVector();
            velocity.y = 0;
            Vector3 diff = target.transform.position - transform.position;
            diff.y = 0;
            diff.Normalize();
            float dotProd = Vector3.Dot(velocity, diff);
            if (dotProd < 0) return 0;
            return (diff * dotProd).magnitude;
        }

        #region Skills

        public bool CastSkill<T>()
        {
            if (!CanCastSkill()) return false;
            if (!Skills.ContainsKey(typeof(T))) return false;
            Skill skill = GetSkill<T>();
            if (!skill.IsOffCooldown(Actor)) return false;
            if (!skill.Cast(Actor)) return false;
            float animTime = skill.GetSkillAnimationTime();
            SetAnimationTime(animTime);
            if(Actor.AnimatorModule != null) Actor.AnimatorModule.SkillCast();
            return true;
        }
        [Button("Add Skill")]
        public void AddAttackSkill(Skill skill)
        {
            if (skill == null) return;

            Skills ??= new Dictionary<Type, Skill>();
            if (Skills.ContainsKey(skill.GetType()))
            {
                //Increase rank
                Skills[skill.GetType()].IncreaseRank();
                return;
            }
            Skills[skill.GetType()] = skill;
            skill.AddToActor(this);
            CalculateManaCosts(); //To be safe, recalculate all
        }

        public Skill GetSkill<T>()
        {
            if (Skills == null || !Skills.ContainsKey(typeof(T))) return null;
            return Skills[typeof(T)];
        }
 
        
        /// <summary>
        /// Removes all attack skills
        /// </summary>
        public void RemoveAllAttackSkills()
        {
            foreach (var key in Skills.Keys)
            {
                Skills[key].RemoveFromActor();
            }
            Skills.Clear();
            CalculateManaCosts();
        }
        
        /// <summary>
        /// Cancels all attack skills without removing them
        /// </summary>
        public void CancelAllAttackSkills()
        {
            foreach (var key in Skills.Keys)
            {
                if(Skills[key] == null) continue;
                Skills[key].Cancel();
            }
        }

        
        /// <summary>
        /// Returns the rank of the skill given by id. If player doesn't have that skill, it will return 0
        /// </summary>
        /// <param name="skillId"></param>
        public int GetSkillRank<T>( )
        {
            Skill skill = GetSkill<T>();
            if (skill == null) return 0; 
            return skill.Rank;
        }
        public void RemoveAttackSkill<T>()
        {
            if (!Skills.ContainsKey(typeof(T))) return;
            Skills.Remove(typeof(T)); 
            CalculateManaCosts();
        }
        
        public void CalculateManaCosts()
        {
            //todo(refactor): Clear this
            //This was before active skill implementation
            ManaCost = 0f;
            return;
            float manaCost = 0;
            foreach (var pair in Skills)
            {
                manaCost += pair.Value.GetEnergyCost();
            }
            ManaCost = manaCost;
        }

        #endregion
        
        #region Animations

        public void SetAnimationTime()
        {
            SetAnimationTime(CurrentAttackPattern.AnimationTime);
        }

        public void SetAnimationTime(float animationTime)
        {
            AnimatorModule animMod = (AnimatorModule)Actor.GetModuleByType(typeof(AnimatorModule));
            if (animMod == null) return;
            animMod.SetAnimationTime(animationTime);
        }
        #endregion

        public override void OnDeath(object sender, EventArgs empty)
        {
            Cancel();
        }
    }
}