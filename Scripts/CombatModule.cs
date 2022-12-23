using System;
using System.Collections;
using System.Collections.Generic;
using Kuantech.Combat;
using Kuantech.Data;
using Kuantech.Inventory;
using Kuantech.Inventory.Items;
using Kuantech.Core.UI;
using Kuantech.Scripts.Managers;
using Kuantech.Utils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Core
{
    public enum AttackTypes
    {
        None,
        Linear,
        Arc,
        Circle,
        Ranged,
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
    
    public class CombatModule : Module
    {
        public static readonly float QueueTimeFactor = 0.5f;
        
        //Components
        public LineTelemetry LineAttackTelemetry;
        public CircleTelemetry CircleAttackTelemetry;
        public ArcTelemetry ArcAttackTelemetry;
        
        //Base Parameters for Unarmed Combat (Or for enemies that doesn't need 'equipment'
        public WeaponAttackPattern DefaultAttackPattern;
        public float DamageTime;
        public float AnimationTime;
        public float Range;
        public float Width;
        public float Angle = 0; //For arc attacks
        public float Knockback;
        public AttackTypes CurrentAttackType;
        public Weapon EquippedWeapon = null;
        public Projectile DefaultProjectilePrefab = null;

        [Header("Attack Positions")] 
        public Transform ShootPosition;
        
        //States
        public float AttackStartTime;
        public bool IsAttacking = false;
        private bool _damageDone = false; //Checks if DamageTime has passed
        private Telemetry ActiveTelemetry;
        
        //Collision
        private Collider[] _results = new Collider[32];
        public LayerMask Targets;

        private UnityAction AttackCompleteHandler;

        private InventoryModule _actorInventory;
        
        //Attack Flow (Combo)
        [Header("Flow")]
        [SerializeField] private int _flowIndex = 0;
        [SerializeField] private float _flowBreakTime = 0.5f;
        private float _lastAttackEndTime = 0f;
        private bool _attackQueued = false;
        
        //AttackSkill
        [SerializeReference] private Dictionary<int, AttackSkill> AttackSkills = new Dictionary<int, AttackSkill>();
        public float ManaCost = 0;
        public bool CanUseSkill = false;
        
        //Events
        public EventHandler<int> AttackStartEvent;
        public EventHandler<int> AttackEvent;
        public EventHandler<Actor> MeleeImpactEvent;
        public EventHandler<ProjectileImpactInfo> RangedImpactEvent;
        public EventHandler<Projectile> ProjectileEndRangeEvent;
        public EventHandler<ProjecitleShotInfo> ProjectileShotEvent;
        
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
        }
        
        public override void Reset()
        {
            IsAttacking = false;
            AttackStartTime = 0;
            _flowIndex = 0;
            _damageDone = false;
            _attackQueued = false;
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
            if (!IsAttacking) return;
            float timePassedAttacking = Time.time - AttackStartTime;
            
            //Check animation time first
            if (timePassedAttacking >= AnimationTime)
            {
                IsAttacking = false; //Ends the attack

                if (!_attackQueued) return;
                _attackQueued = false;
                Attack();
                return;
            }

            if (_damageDone) return;
            float normalizedTime = timePassedAttacking / DamageTime;
            
            if(ActiveTelemetry != null) ActiveTelemetry.SetFill(normalizedTime);
            if (normalizedTime > 1f)
            {
                _lastAttackEndTime = Time.time;
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
                }
                AttackCompleteHandler?.Invoke();
                _damageDone = true;
            }
        }

        public void ApplyAttackPattern(WeaponAttackPattern pattern)
        {
            //First parameters...
            Range = pattern.Range;
            Angle = pattern.Angle;
            AnimationTime = pattern.AnimationTime;
            DamageTime = pattern.DamageTime;
            Width = pattern.Width;
            Knockback = pattern.Knockback;
            
            //...then attack type
            SetAttackType(pattern.AttackType);
            SetAnimationTime(); //For clients
        }
        
        public bool Attack(UnityAction attackCompleteHandler = null)
        {
            if (IsAttacking)
            {
                float queueTime = (Time.time - AttackStartTime) / AnimationTime;
                //Check current frame
                if (queueTime >= AnimationTime * QueueTimeFactor)
                {
                    //Queue next attack
                    _attackQueued = true;
                }
                return false;
            }
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
                ApplyAttackPattern(EquippedWeapon.AttackPatterns[_flowIndex]);
            }
            else
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
                Actor.Energy -= ManaCost;
                CanUseSkill = true;
            }
             
            AttackStartEvent?.Invoke(this, _flowIndex);
            AnimatorModule animMod = (AnimatorModule)Actor.GetModuleByType(typeof(AnimatorModule));
            if (animMod != null)
            {
                animMod.LightAttackTrigger(attackIndex:GetFlowIndex(), handIndex:0); //todo: Consider supporting off hand weapons
            }

            return true;
        }

        public int GetFlowIndex()
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
                ActiveTelemetry.SetAngle(Angle);
                ActiveTelemetry.SetLength(Range);
                ActiveTelemetry.SetWidth(Width);
                ResetActiveTelemetry();
            }
        }
        
        #region AttackMethods
        
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
                    return forwardDistrSqr <= Range * Range && widthSqr <= (Width * Width * 0.5f * 0.5f) && Vector3.Dot(transform.forward, diffVector) >= 0;
                case AttackTypes.Arc:
                    bool pointInAngle = transform.PointIsInAngleRange(target, Angle);
                    bool isInRange = sqrDist <= Range * Range;
                    return pointInAngle && isInRange;
                case AttackTypes.Ranged:
                case AttackTypes.Circle:
                    return sqrDist <= Range * Range;
                default:
                    return false;
            }
        }
        
        public float GetDamage()
        {
            if (EquippedWeapon != null) return EquippedWeapon.GetDamage(_flowIndex);
            return DefaultAttackPattern.Damage;
        }

        public Vector3 GetShootPosition()
        {
            return ShootPosition != null ? ShootPosition.position : transform.position;
        }
        //Attack Casts
        public static void DealAreaDamage(CombatModule from, float damage, float range, float knockback, bool useSkill, float angle=0f)
        {
            Vector3 center = from.transform.position + Vector3.up;
            Collider[] results = new Collider[32];
            int hitCount = UnityEngine.Physics.OverlapSphereNonAlloc(center, range, results, from.Targets);
            Vector3 forwardVector = from.transform.forward;
            forwardVector.y = 0f;
            for (int i = 0; i < hitCount; ++i)
            {
                if(results[i] == null) continue;
                
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
                if(Vector3.Angle(forwardVector, colliderCorners[0] - from.transform.position) > 90f) continue;
                
                float _minAngle = 0f;
                float _maxAngle = 0f;
                bool _minFlag = false;
                bool _maxFlag = false;
                float halfAngle = angle * 0.5f + 1f;
                foreach (var corner in colliderCorners)
                {
                    //Check angle
                    Vector3 differenceVector = corner - from.transform.position;
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
                    if(Mathf.Abs(angleOfCollider) <= halfAngle) isInAngleRange = true; //1f for safety check
                }
                
                //We still need to check if all angles were outside (if collider is too big and all angles are outside the range
                if (_maxFlag && _minFlag && _minAngle <= -halfAngle && _maxAngle >= halfAngle) isInAngleRange = true;
                
                if(!isInAngleRange) continue;
                Actor target = results[i].GetComponent<Actor>();
                if(target == null) continue;
                from.DamageActorMelee(target, damage, knockback);   
            }
        }
        
        private void LinearMeleeAttack()
        {
            Vector3 center = transform.position + transform.forward * Range*0.5f;
            center.y = 1f;
            int hitCount = UnityEngine.Physics.OverlapBoxNonAlloc(center, new Vector3(Width*0.5f, Width*0.5f, Range*0.5f), 
                _results, Quaternion.identity, Targets.value);

            for (int i = 0; i < hitCount; ++i)
            {
                if(_results[i] == null) continue;
                Actor target = _results[i].GetComponent<Actor>();
                if(target == null) continue;
                DamageActorMelee(target,GetDamage(), Knockback);
                if (CanUseSkill)
                {
                    MeleeImpactEvent?.Invoke(this, target);
                }
            }
        }
        private void ArcMeleeAttack()
        {
            ArcMeleeAttack(Angle);
        }

        private void ArcMeleeAttack(float angle)
        {
            DealAreaDamage(this, GetDamage(), Range, Knockback, true, angle);
        }

        private void CircleMeleeAttack()
        {
            ArcMeleeAttack(360f);
        }

        private void RangedAttack()
        {
            GameObject projectilePrefab = null;
            if (EquippedWeapon != null)
            {
                projectilePrefab = EquippedWeapon.ProjectilePrefab;
            }
            if (DefaultProjectilePrefab != null)
            {
                projectilePrefab = DefaultProjectilePrefab.gameObject;
            }

            if (projectilePrefab == null)
            {
                Debug.LogError("Tried to shoot projectile with no default projectile and null equipped weapon");
                return;
            }
            ShootProjectile(EquippedWeapon, projectilePrefab, 
                GetShootPosition(), 
                transform.forward,
                true);
        }

        /// <summary>
        /// Shoots a projectile from the weapon
        /// </summary>
        /// <param name="weapon"></param>
        /// <param name="projectilePrefab"></param>
        /// <param name="castSkill"></param>
        public Projectile ShootProjectile(Weapon weapon, GameObject projectilePrefab, Vector3 shootPosition, Vector3 direction, bool castSkill)
        {
            if (projectilePrefab == null) return null;
            PrefabPool pool = GameManager.Instance.Pool;
            GameObject projectileObj = pool.GetObject(projectilePrefab);
            //Vector3 shootPosition = ShootPosition != null ? ShootPosition.position : transform.position;
            if (projectileObj.TryGetComponent(out Projectile projectile))
            {
                projectile.Initialize(this, weapon);
                projectile.Targets = Targets;
                projectile.Range = Range;
                if (CanUseSkill && castSkill)
                {
                    ProjectileShotEvent?.Invoke(this, new ProjecitleShotInfo()
                    {
                        Projectile = projectile,
                        Direction = direction,
                        From = shootPosition,
                    });
                }
                
                projectileObj.transform.position = shootPosition;
                projectile.transform.rotation = Quaternion.LookRotation(direction);
                projectileObj.SetActive(true);
                return projectile;

            }
            else
            {
                pool.PoolObject(projectileObj);
                return null;
            }
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
            float finalDamage = GetDamage();
            target.ReceiveDamage(Actor, finalDamage);
        }
        #endregion

        public void DamageActorMelee(Actor target, float damage, float knockback)
        {
            if (target.Health <= 0f) return;
            target.ReceiveDamage(Actor, damage);
            
            //Get currentSpeed
            float momentum = 0f;
            if (target.MovementModule != null)
            {
                momentum = Actor.MovementModule.GetForwardMovement() * Actor.MovementModule.GetForwardSpeed();
                momentum = Mathf.Max(0f, momentum); //Backward movement shouldn't contribute
            }
            
            KnockbackActor(target, knockback + momentum);
        }

        /// <summary>
        /// Applies a knockback to target
        /// </summary>
        /// <param name="target">Actor to apply knockback</param>
        /// <param name="knockback">Amount of knockback force</param>
        private void KnockbackActor(Actor target, float knockback)
        {
            //todo: calculate knockback from main stat
            StartCoroutine(KnockbackRoutine(target, knockback));
        }

        private IEnumerator KnockbackRoutine(Actor target, float knockback)
        {
            Vector3 diff = target.transform.position - transform.position;
            diff.y = 0f;
            diff.Normalize();
            diff *= knockback;
            target.ForceMoveVector += diff;
            yield return new WaitForSeconds(0.25f);
            target.ForceMoveVector -= diff;
        }

        #region AttackSkills
        
        [Button("Add Skill")]
        public void AddAttackSkill(int skillId)
        {
            AttackSkill skill = Spellbook.Instance.GetAttackSkill(skillId);
            if (skill == null) return;
            AddAttackSkill(skill);
        }
        
        public void AddAttackSkill(AttackSkill skill)
        {
            AttackSkills ??= new Dictionary<int, AttackSkill>();
            if (AttackSkills.ContainsKey(skill.Id))
            {
                //Increase rank
                AttackSkills[skill.Id].IncreaseRank();
                return;
            }
            AttackSkills[skill.Id] = skill;
            skill.Initialize(this);
            CalculateManaCosts(); //To be safe, recalculate all
        }

        public bool CanCastSkill()
        {
            return CanUseSkill;
        }
        
        public void RemoveAttackSkill(AttackSkill skill)
        {
            RemoveAttackSkill(skill.Id);
        }

        public void RemoveAllAttackSkills()
        {
            foreach (var key in AttackSkills.Keys)
            {
                AttackSkills[key].Remove();
            }
            AttackSkills.Clear();
            CalculateManaCosts();
        }
        /// <summary>
        /// Returns the rank of the skill given by id. If player doesn't have that skill, it will return 0
        /// </summary>
        /// <param name="skillId"></param>
        public int GetSkillRank(int skillId)
        {
            if (AttackSkills.ContainsKey(skillId))
            {
                return AttackSkills[skillId].Rank;
            }
            return 0;
        }
        public void RemoveAttackSkill(int skillId)
        {
            if (!AttackSkills.ContainsKey(skillId)) return;
            AttackSkills[skillId].Remove();//Unsubscribe from events
            AttackSkills.Remove(skillId); 
            CalculateManaCosts();
        }
        
        public void CalculateManaCosts()
        {
            float manaCost = 0;
            foreach (var pair in AttackSkills)
            {
                manaCost += pair.Value.GetEnergyCost();
            }
            ManaCost = manaCost;
        }

        #endregion
        
        #region Animations

        public void SetAnimationTime()
        {
            AnimatorModule animMod = (AnimatorModule)Actor.GetModuleByType(typeof(AnimatorModule));
            if (animMod == null) return;
            animMod.SetAnimationTime(AnimationTime);
        }
        #endregion

        public override void OnDeath(object sender, EventArgs empty)
        {
            Cancel();
        }
    }
}