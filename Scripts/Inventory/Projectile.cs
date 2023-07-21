using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.FX;
using Kuantech.Inventory.Items;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

namespace Kuantech.Combat
{
    public class Projectile : MonoBehaviour
    {
        [Header("Properties")]
        public float Speed;
        public float Range;
        [SerializeField] protected float RiseHeight;
        public float FollowLerpFactor;
        public float Knockback;
        public float KnockbackTime;
        public const float ReachThreshold = 0.1f;

        public bool RawDamage = false;
        public CombatModule CastBy;
        public Weapon ShotFrom = null;
        public bool DestroyOnImpact = true;
        public LayerMask Targets;
        public float DespawnDelay;
        
        [Header("Visuals")]
        public GameObject Visual;
        public TrailRenderer TrailRenderer;
        
        public Collider Collider;
        private bool _despawned = false;
        public delegate void ImpactOverrideDelegate(Projectile proj, Actor target, GameObject gameObjects);

        public ImpactOverrideDelegate ImpactOverride;
        // Behaviour flags
        public float Damage = 1;
        public float splashRadius = 0f; // 0 means no splash
        
        protected float _age = 0f; // Age of the projectile in terms of seconds
        protected float _lifeTime = 0f;

        public List<GameObject> Attachments;

        [Header("Effects")] 
        public AudioSource StartSound;
        public AudioSource ImpactSound;

        private Vector3 _shotPosition;
        
        //Target variables
        private Transform _target;
        private float _targetHeightOffset = 1.5f;
        private float _InitialDistanceToTarget = 0f;
        
        //Events
        public EventHandler InitializeEvent;
        public EventHandler DespawnEvent;

        private bool _targeted;
        private Vector3 _lastDirection;
        /// <summary>
        /// Initializes and shoots the projectile
        /// </summary>
        /// <param name="castBy">CombatModule that shoots the projectile. If null, damage will be calculated from projectile properties</param>
        /// <param name="shotFrom">Weapon that this projectile is shot from. If null, damage will be calculated from default attack pattern or projectile properties</param>
        /// <param name="target">Target transform. If set to non-null, proectile will follow the target</param>
        /// <param name="riseHeight">To act as pseudo throwable. Projectile will rise to this height and falls down in a sinudoidal fasion.</param>
        public void Initialize(CombatModule castBy, Weapon shotFrom, Vector3 shootPosition, Quaternion shootRotation, Transform target = null)
        {
            //Set pos and rot
                            
            transform.position = shootPosition;
            transform.rotation = shootRotation;
            
            CastBy = castBy;
            ShotFrom = shotFrom;
            ImpactOverride = null;
            DestroyOnImpact = true;
            if (castBy != null)
            {
                Targets = castBy.Targets;
                Range = castBy.CurrentAttackPattern.Range;
                Knockback = castBy.CurrentAttackPattern.Knockback;
                KnockbackTime = castBy.CurrentAttackPattern.KnockbackTime;
                Damage = castBy.GetDamage();
            }
            _age = 0f;
            
            if (StartSound != null)
            {
                StartSound.time = 0f;
                StartSound.Play();
            }
            
            _InitialDistanceToTarget = Range;
            _target = target;
            _targeted = _target != null;
            if (_target != null)
            {
                Vector3 diffToTarget = (target.transform.position - _shotPosition);
                _lastDirection = diffToTarget;
                _InitialDistanceToTarget = diffToTarget.magnitude;
                _lifeTime = 100.0f; //todo: This should be handled better
            }
            else
            {
                _lifeTime = Range / Speed;
            }
            
            _despawned = false;
            _shotPosition = shootPosition;
            if (Collider != null) Collider.enabled = true;
            if(Visual != null) Visual.SetActive(true);
            InitializeEvent?.Invoke(this, EventArgs.Empty);
            _newPosition = transform.position;
            if(TrailRenderer != null) TrailRenderer.Clear();
        }

        private Vector3 _newPosition; //Need a seperate variable that doesn't include rise height values
        protected virtual void Update()
        {
            if (_despawned) return;
            if (_targeted && _target == null || Speed == 0f)
            {
                Despawn();
                return;
            }
            Vector3 moveDirection = transform.forward;
            if (_targeted)
            {
                Vector3 dist = _target.transform.position - transform.position;
                dist.y = 0;
                float dot = Vector3.Dot(dist, _lastDirection);
                float sqrMag = dist.sqrMagnitude;
                Vector3 forwardDir = transform.forward;
                forwardDir.y = 0;
                _lastDirection.y = 0;
                bool reachedTarget = dot < 0 || sqrMag <= ReachThreshold*ReachThreshold;
                //Check Target Reach Distance
                if (reachedTarget && _target.gameObject.activeInHierarchy)
                {
                    HandleOnTriggerEnter(_target.gameObject);
                    Despawn();
                    return;
                }
                
                if (_target.gameObject.activeInHierarchy)
                {
                    if (sqrMag > (ReachThreshold*ReachThreshold))
                    {
                        moveDirection = dist.normalized;
                        transform.forward = Vector3.Lerp(moveDirection, dist, Time.deltaTime * FollowLerpFactor);
                    }
                }
            }
      
            
            //Act like targeted throwable. For actual throwable, see throwable class
            Vector3 horizontalDiff = (transform.position - _shotPosition);
            horizontalDiff.y = 0f;
            float normalizedHeight = Mathf.Clamp01(horizontalDiff.magnitude / _InitialDistanceToTarget);
            float throwbleHeightAddition = Mathf.Sin(normalizedHeight * Mathf.PI) * RiseHeight;
            _newPosition += transform.forward * Time.deltaTime * Speed;
            _lastDirection = transform.forward;
            transform.position = _newPosition + Vector3.up * throwbleHeightAddition;
            
            //Check lifetime
            _age += Time.deltaTime;
            if (_age > _lifeTime)
            {
                Despawn();
            }
            
         
        }

        public void SetTarget(Transform target)
        {
            _target = target;
        }
        
        public void AddAttachment(GameObject component)
        {
            Attachments.Add(component);
            component.transform.SetParent(transform);
            component.transform.localPosition = Vector3.zero;
            component.transform.localRotation = Quaternion.identity;
        }

        public void AddEffect(EffectTypes effectType)
        {
            Effect effect = EffectsLibrary.Instance.PlayEffect(effectType, Vector3.zero, Quaternion.identity);
            AddAttachment(effect.gameObject);
            effect.gameObject.transform.localPosition = Vector3.zero;
            effect.gameObject.transform.localRotation = Quaternion.identity;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            //If in filtered targets...
            if (!((Targets.value & (1 << other.gameObject.layer)) > 0)) return;
            HandleOnTriggerEnter(other.gameObject);
        }

        private void HandleOnTriggerEnter(GameObject triggeredObject)
        {
                  
            //todo: Apply knockback
            if (CastBy != null && triggeredObject == CastBy.gameObject) return; //Don't trigger for the caster

            Actor targetActor = triggeredObject.GetComponent<Actor>();
            if (targetActor != null && 
                (targetActor.Health <= 0f || 
                 (CastBy != null && targetActor.FactionId == CastBy.Actor.FactionId))) return; //Don't attack actor with same faction
            
            if (ImpactSound != null)
            {
                ImpactSound.Play();
            }
            
            if (ImpactOverride != null)
            {
                ImpactOverride(this, targetActor, triggeredObject.gameObject);
                return;
            }
            
            if (splashRadius > 0)
            {
                Vector3 origin = transform.position;
                Collider[] colliders = UnityEngine.Physics.OverlapSphere(origin, splashRadius);
                foreach (Collider coll in colliders)
                {
                    Impact(coll.gameObject);    
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
        private void Impact(GameObject impacted)
        {
            Actor target = impacted.GetComponent<Actor>();
            
            CombatModule.KnockbackActor(CastBy, target, transform.forward, Knockback, KnockbackTime);

            if (CastBy == null)
            {
                if(target != null) target.ReceiveDamage(null, Damage, RawDamage); //Cast from something that is not an Actor
                return;
            }
            if (target == null || target == CastBy.Actor) return;
            CastBy.OnProjectileImpact(this, target);
        }

        private void ClearAttachments()
        {
            foreach (var attachment in Attachments)
            {
                GameManager.Instance.Pool.PoolObject(attachment);
            }
            Attachments.Clear();
        }
        
        public virtual void Despawn()
        {
            _despawned = true;
            _age = 0f;
            DespawnEvent?.Invoke(this, EventArgs.Empty);
            ClearAttachments();
            if (DespawnDelay <= 0)
            {
                GameManager.Instance.Pool.PoolObject(gameObject);
                return;
            }
            if (Collider != null) Collider.enabled = false;
            if(Visual != null) Visual.SetActive(false);
            GameManager.Instance.PoolObjectAfterTime(gameObject, DespawnDelay);
        }
    }
}