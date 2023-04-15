using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.FX;
using Kuantech.Inventory.Items;
using UnityEngine;

namespace Kuantech.Combat
{
    public class Projectile : MonoBehaviour
    {
        [Header("Properties")]
        public float Speed;
        public float Range;
        public float Knockback;
        public float KnockbackTime;
        public bool RawDamage = false;
        public CombatModule CastBy;
        public Weapon ShotFrom = null;
        public bool DestroyOnImpact = true;
        public LayerMask Targets;
        public float DespawnDelay;
        
        [Header("Visuals")]
        public GameObject Visual;
        public Collider Collider;
        private bool _despawned = false;
        public delegate void ImpactOverrideDelegate(Projectile proj, Actor target, GameObject gameObjects);

        public ImpactOverrideDelegate ImpactOverride;
        // Behaviour flags
        public float Damage = 1;
        public float splashRadius = 0f; // 0 means no splash
        
        protected float _age = 0f; // Age of the projectile in terms of seconds

        public List<GameObject> Attachments;

        [Header("Effects")] 
        public AudioSource StartSound;
        public AudioSource ImpactSound;

        private Actor _target;
        private float _lerpFactor;
        
        //Events
        public EventHandler InitializeEvent;
        public EventHandler DespawnEvent;

        public void Initialize(CombatModule castBy, Weapon shotFrom)
        {
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
            _target = null;
            _despawned = false;
            if (Collider != null) Collider.enabled = true;
            if(Visual != null) Visual.SetActive(true);
            InitializeEvent?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void Update()
        {
            if (_despawned) return;
            if(Speed == 0) Despawn();

            if (_target != null && _target.gameObject.activeInHierarchy && _target.Health > 0)
            {
                Vector3 dist = _target.transform.position - transform.position;
                dist.y = 0;
                dist.Normalize();
                transform.forward = Vector3.Lerp(transform.forward, dist, Time.deltaTime * _lerpFactor);
            }
            
            transform.position += transform.forward * Time.deltaTime * Speed;
            _age += Time.deltaTime;
            if (_age > Range/Speed)
            {
                Despawn();
            }
        }
        
        public void SetTarget(Actor target, float followLerpFactor)
        {
            _target = target;
            _lerpFactor = followLerpFactor;
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
            
            //todo: Apply knockback
            if (CastBy != null && other.gameObject == CastBy.gameObject) return; //Don't trigger for the caster

            Actor targetActor = other.GetComponent<Actor>();
            if (targetActor != null && targetActor.Health <= 0f) return;
            
            if (ImpactSound != null)
            {
                ImpactSound.Play();
            }
            
            if (ImpactOverride != null)
            {
                ImpactOverride(this, targetActor, other.gameObject);
                return;
            }
            
            if (splashRadius > 0)
            {
                Vector3 origin = other.ClosestPoint(transform.position);
                Collider[] colliders = UnityEngine.Physics.OverlapSphere(origin, splashRadius);
                foreach (Collider coll in colliders)
                {
                    Impact(coll.gameObject);    
                }
            }
            else
            {
                // Just hurt the collided
                Impact(other.gameObject);
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