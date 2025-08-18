using System.Collections.Generic;
using Kuantech.Core.Combat;
using Kuantech.Core.FX;
using Kuantech.Rpg.Inventory;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Kuantech.Core
{
    public class Projectile : MonoBehaviour
    {
        [Header("World Direction")]
        public Vector3 WorldForward = Vector3.up;
        public Vector3 WorldUp = Vector3.up;
        
        [Header("Properties")] 
        public string ProjectileId;
        public bool Is2D = false;
        public float Speed;
        public float Range;
        
        [Header("Fake Throwable")]
        [SerializeField] protected float RiseHeight;
        [Tooltip("Amount of seconds to reach the peak")]
        [SerializeField] protected float RiseTime; 
        [SerializeField] private bool UseRiseScaleFactor = false;
        [Tooltip("For top down 2d throwables, the visual will scale to this at max height. Must be greter than 0")]
        [SerializeField] protected float RiseScaleFactor; 
        
        public float TargetFollowSlerpFactor;
        public float Knockback;
        public float KnockbackTime;
        public float ReachThreshold = 0.1f;
        public bool ZeroYDiff = true;
        public float MaxLifetime = 20f; //Maximum duration a projectile can be alive, even targeted

        public bool RawDamage = false;
        public Actor CastBy;
        public Weapon ShotFrom = null;
        public bool DestroyOnImpact = true;
        public LayerMask Targets;
        public float DespawnDelay;
        
        [Header("Visuals")]
        public GameObject Visual;
        public TrailRenderer TrailRenderer;
        
        public Collider Collider;
        public Collider2D Collider2D;
        
        protected bool Despawned = false;
        public delegate void ImpactOverrideDelegate(Projectile proj, Actor target, GameObject gameObjects);

        public ImpactOverrideDelegate ImpactOverride;
        
        // Behaviour flags
        public DamageInfo Damage;
        public float SplashRadius = 0f; // 0 means no splash
        public DamageInfo SplashDamage;
        
        //Runtime
        protected float _age = 0f; // Age of the projectile in terms of seconds
        protected float _lifeTime = 0f;
        protected float CurrentSpeed;

        public List<GameObject> Attachments;

        [Header("Effects")] 
        public EffectPlayer StartEffect;
        public EffectPlayer LifetimeEndEffect;
        public EffectPlayer ImpactEffect;

        
        //Target variables
        protected Transform Target;
        private float _targetHeightOffset = 1.5f;
        private float _InitialDistanceToTarget = 0f;
        
        //Events
        public UnityAction<Projectile> ShotEvent;
        public UnityAction<Projectile> LifetimeEndEvent;
        public UnityAction<Projectile> OnImpactEvent;
    
        //Runtime
        private bool _targeted;
        private Vector3 _shotPosition;
        private Vector3 _direction;
        private Vector3 _targetOffset;
        public HashSet<int> FactionFilter;
        
        /// <summary>
        /// Initializes and shoots the projectile
        /// </summary>
        /// <param name="castBy">CombatModule that shoots the projectile. If null, damage will be calculated from projectile properties</param>
        /// <param name="shotFrom">Weapon that this projectile is shot from. If null, damage will be calculated from default attack pattern or projectile properties</param>
        /// <param name="target">Target transform. If set to non-null, proectile will follow the target</param>
        /// <param name="riseHeight">To act as pseudo throwable. Projectile will rise to this height and falls down in a sinudoidal fasion.</param>
        public virtual void Shoot(Actor castBy, Weapon shotFrom, Vector3 shootPosition, Vector3 shootDirection, Transform target = null, float relativeSpeed = 0.0f)
        {
            //Set pos and rot
            Reset();
            _direction = CancelUpComponent(shootDirection).normalized;
            _targetOffset = Vector3.zero;
            transform.position = shootPosition;
            Quaternion rot = GetForwardRotation(_direction);
            transform.rotation = rot;
            
            if (Visual != null)
            {
                Visual.transform.localScale = Vector3.one;
            }
            
            CastBy = castBy;
            ShotFrom = shotFrom;
            ImpactOverride = null;
            DestroyOnImpact = true;
            CurrentSpeed = Speed + relativeSpeed;

            StartEffect.PlayEffectAtPosition(transform.position, Quaternion.identity);
            
            ShotEvent?.Invoke(this);
            
            //Targeted
            _InitialDistanceToTarget = Range;
            Target = target;
            _targeted = Target != null;
            if (Target != null)
            {
                Vector3 diffToTarget = (GetTargetPosition() - _shotPosition);
                _direction = CancelUpComponent(diffToTarget).normalized;
                _InitialDistanceToTarget = diffToTarget.magnitude;
                _lifeTime = MaxLifetime; //todo: This should be handled better
            }
            else
            {
                _lifeTime = Range / CurrentSpeed;
            }
            
            Despawned = false;
            _shotPosition = shootPosition;
            ToggleCollider(true);
            if(Visual != null) Visual.SetActive(true);
            _newPosition = transform.position;
            if(TrailRenderer != null) TrailRenderer.Clear();
        }

        public void SetTargetOffset(Vector3 targetOffset)
        {
            _targetOffset = targetOffset;
        }
        
        private Vector3 GetTargetPosition()
        {
            if (Target == null) return Vector3.zero;
            return Target.transform.position + _targetOffset;
        }
        
        private Vector3 CancelUpComponent(Vector3 direction)
        {
            if (ZeroYDiff)
            {
                //We can remove the world up component of direction in the future
                return direction;
            }
            else
            {
                return direction;
            }
            
        }
        protected Quaternion GetForwardRotation(Vector3 direction)
        {
            
            return Helpers.GetRotationFromWorldForward(direction, WorldUp, WorldForward);
        }
        
        private Vector3 _newPosition; //Need a seperate variable that doesn't include rise height values
        protected virtual void Update()
        {
            if (Despawned) return;
            if (_targeted && Target == null || CurrentSpeed == 0f)
            {
                Despawn();
                return;
            }

            if (_targeted && !Target.gameObject.activeInHierarchy)
            {
                Despawn();
                return;
            }

            if (_targeted)
            {
                Vector3 diffToTarget = GetTargetPosition() - transform.position;
                diffToTarget = CancelUpComponent(diffToTarget);
                float sqrMag = diffToTarget.sqrMagnitude;

                bool reachedTarget = sqrMag <= ReachThreshold*ReachThreshold;
                //Check Target Reach Distance
                if (reachedTarget && Target.gameObject.activeInHierarchy)
                {
                    HandleOnTriggerEnter(Target.gameObject);
                    Despawn();
                    return;
                }
                
                if (Target.gameObject.activeInHierarchy)
                {
                    if (sqrMag > (ReachThreshold*ReachThreshold))
                    {
                        //moveDirection = dist.normalized;
                        Quaternion targetRot = GetForwardRotation(diffToTarget);
                        Quaternion currentRot = Quaternion.Slerp(transform.rotation, targetRot,
                            Time.deltaTime * TargetFollowSlerpFactor);
                        transform.rotation = currentRot;
                        _direction = diffToTarget.normalized;
                    }
                }
            }
            
            //Act like targeted throwable. For actual throwable, see throwable class
            float throwbleHeightAddition = GetThrowableHeightAddition();
            _newPosition += _direction * Time.deltaTime * CurrentSpeed;
            SetHeightScale(GetNormalizedHeight());
            transform.position = _newPosition + WorldUp * throwbleHeightAddition;
            CheckLifetime();
        }
        
        
        protected virtual float GetNormalizedHeight()
        {
            if (RiseTime == 0.0f) return 0f;
            if (_age < RiseTime)
            {
                return _age / RiseTime;
            }
            return Mathf.Clamp01(1.0f - (_age - RiseTime));
        }
        
        protected virtual float GetThrowableHeightAddition()
        {
            float normalizedHeight = GetNormalizedHeight();
            return Mathf.Sin(normalizedHeight * Mathf.PI) * RiseHeight;
        }
        
        /// <summary>
        /// Sets the height scale for top down 2d throwables
        /// </summary>
        /// <param name="normalizedHeight"></param>
        protected virtual void SetHeightScale(float normalizedHeight)
        {
            if (!UseRiseScaleFactor) return;
            if (Visual != null)
            {
                Visual.transform.localScale = Vector3.one * Mathf.Lerp(1, RiseScaleFactor, normalizedHeight);
            }
        }
        protected void CheckLifetime()
        {
            if (Despawned) return;
            //Check lifetime
            _age += Time.deltaTime;
            if (_age > Mathf.Min(_lifeTime, MaxLifetime))
            {
                EndLifetime();
            }
        }

        private void EndLifetime()
        {
            LifetimeEndEvent?.Invoke(this);
            LifetimeEndEffect?.PlayEffectAtPosition(transform.position, Quaternion.identity);
            Despawn();
        }
        
        public void SetTarget(Transform target)
        {
            Target = target;
        }
        
        public void AddAttachment(GameObject component)
        {
            Attachments.Add(component);
            component.transform.SetParent(transform);
            component.transform.localPosition = Vector3.zero;
            component.transform.localRotation = Quaternion.identity;
        }

        public void AddEffect(int effectType)
        {
            Effect effect = EffectsLibrary.GetContext<EffectsLibrary>().PlayEffect(effectType, Vector3.zero, Quaternion.identity);
            AddAttachment(effect.gameObject);
            effect.gameObject.transform.localPosition = Vector3.zero;
            effect.gameObject.transform.localRotation = Quaternion.identity;
        }
        
        #region Colliders

        public void ToggleCollider(bool toggle)
        {
            if (Collider != null)
            {
                Collider.enabled = toggle;
            }

            if (Collider2D != null)
            {
                Collider2D.enabled = toggle;
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            //If in filtered targets...
            if (!((Targets.value & (1 << other.gameObject.layer)) > 0)) return;
            HandleOnTriggerEnter(other.gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            //If in filtered targets...
            if (!((Targets.value & (1 << other.gameObject.layer)) > 0)) return;
            HandleOnTriggerEnter(other.gameObject);
        }

        protected virtual void HandleOnTriggerEnter(GameObject triggeredObject)
        {
            if (CastBy != null && triggeredObject == CastBy.gameObject) return; //Don't trigger for the caster

            Actor targetActor = triggeredObject.GetComponent<Actor>();
            if (targetActor != null && 
                (!targetActor.IsAlive())) return; //Don't attack actor with same faction
            
            //Check factions Ids
            if (CastBy.IsAlly(targetActor))
            {
                return; //Don't damage actor of same faction
            }
            
            ImpactEffect.PlayEffectAtPosition(transform.position, Quaternion.identity);
            OnImpactEvent?.Invoke(this);
            
            if (ImpactOverride != null)
            {
                ImpactOverride(this, targetActor, triggeredObject.gameObject);
                return;
            }
            
            if (SplashRadius > 0)
            {
                Vector3 origin = transform.position;
                DamageInfo splashDamage = SplashDamage;
                HitInfo hitInfo = new HitInfo()
                {
                    DamageInfo = splashDamage,
                    Hitter = CastBy != null ? CastBy.gameObject : null,
                    HitDirection = _direction,
                    KnockbackDuration = KnockbackTime,
                    KnockbackForce = Knockback,
                };
                if (Is2D)
                {
                    CombatUtilities.HitActorsInCircle2D(origin, SplashRadius, Targets, hitInfo, FactionFilter);
                }
                else
                {
                    // 3D
                    Collider[] colliders3D = UnityEngine.Physics.OverlapSphere(origin, SplashRadius);
                    foreach (Collider coll in colliders3D)
                    {
                        Impact(coll.gameObject);
                    }
                }
            }
            else
            {
                // Just hurt the collided
                Impact(triggeredObject);
            }
            if (DestroyOnImpact)
            {
                
                Despawn();
            }
        }
        #endregion
        
        protected virtual void Impact(GameObject impacted)
        {
            Actor target = impacted.GetComponent<Actor>();
            GameObject hitter = CastBy != null ? CastBy.gameObject : null;
            if (target != null)
            {
                target.OnHit(new HitInfo()
                {
                    DamageInfo = Damage,
                    Hitter = hitter,
                    HitDirection = _direction,
                    KnockbackDuration = KnockbackTime,
                    KnockbackForce = Knockback,
                });
     
            }
        }

        private void ClearAttachments()
        {
            foreach (var attachment in Attachments)
            {
                PoolManager.PoolObject(attachment);
            }
            Attachments.Clear();
        }
        
        public virtual void Despawn()
        {
            if (Despawned)
            {
                return;
            }
            Despawned = true;
            _age = 0f;
            ClearAttachments();
            if (DespawnDelay <= 0)
            {
                PoolManager.PoolObject(gameObject);
                return;
            }

            ToggleCollider(false);
            if(Visual != null) Visual.SetActive(false);
            PoolManager.PoolObject(gameObject, DespawnDelay);
        }

        public void Reset()
        {
            Despawned = false;
            _age = 0f;
        }
    }
}