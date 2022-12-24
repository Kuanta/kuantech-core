using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.FX;
using Kuantech.Inventory.Items;
using UnityEngine;

namespace Kuantech.Combat
{
    public class Projectile : MonoBehaviour
    {
        public float Speed;
        public float Range;
        public CombatModule CastBy;
        public Weapon ShotFrom = null;
        public bool DestroyOnImpact = true;
        public LayerMask Targets;
        
        public delegate void ImpactOverrideDelegate(Projectile proj, Actor target);

        public ImpactOverrideDelegate ImpactOverride;
        // Behaviour flags
        public float Damage = 1;
        public float splashRadius = 0f; // 0 means no splash
        
        private float _age = 0f; // Age of the projectile in terms of seconds

        public List<GameObject> Attachments;

        [Header("Effects")] 
        public AudioSource StartSound;
        public AudioSource ImpactSound;
        
        public void Initialize(CombatModule castBy, Weapon shotFrom)
        {
            CastBy = castBy;
            ShotFrom = shotFrom;
            ImpactOverride = null;
            DestroyOnImpact = true;
            _age = 0f;
            if (StartSound != null)
            {
                StartSound.Play();
            }
        }

        private void Update()
        {
            if(Speed == 0) Despawn();
            transform.position = transform.position + transform.forward * Time.deltaTime * Speed;
            _age += Time.deltaTime;
            if (_age > Range/Speed)
            {
                Despawn();
            }
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
            //todo: Apply knockback
            if (CastBy == null || other.gameObject == CastBy.gameObject) return;
            
            //If in filtered targets...
            if (!((Targets.value & (1 << other.gameObject.layer)) > 0)) return;

            Actor targetActor = other.GetComponent<Actor>();
            if (targetActor != null && targetActor.Health <= 0f) return;
            
            if (ImpactSound != null)
            {
                ImpactSound.Play();
            }
            
            if (ImpactOverride != null)
            {
                ImpactOverride(this, targetActor);
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
            if (CastBy == null) return;
            if (!impacted.TryGetComponent(out Actor target)) return;
            if (target == CastBy.Actor) return;
            if(CastBy.CanUseSkill) CastBy.OnProjectileImpact(this, target);
        }

        private void ClearAttachments()
        {
            foreach (var attachment in Attachments)
            {
                GameManager.Instance.Pool.PoolObject(attachment);
            }
            Attachments.Clear();
        }
        
        public void Despawn()
        {
            _age = 0f;
            ClearAttachments();
            GameManager.Instance.Pool.PoolObject(gameObject);
        }
    }
}