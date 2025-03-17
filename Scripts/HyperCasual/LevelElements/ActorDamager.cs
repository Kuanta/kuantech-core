using Kuantech.Core.Combat;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public class ActorDamager : MonoBehaviour
    {
        [SerializeField] private int FactionId = 99;
        [SerializeField] private float Damage = 0.1f;
        [SerializeField] private bool PercentageDamage = true;
        [SerializeField] private bool RawDamage = true;
        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent(out IHittable actor)) return;
            if (!actor.CanBeHit()) return;
            DealDamage(actor);
        }

        protected virtual void DealDamage(IHittable hittable)
        {
            float damage = 0;
            if (hittable is Actor actor && PercentageDamage)
            {
                HealthcareModule hm = actor.GetModule<HealthcareModule>();
                if (hm != null)
                {
                    float maxHealth = hm.GetMaxHealth();
                        damage = maxHealth * Mathf.Clamp01(Damage);
                }
            }
            damage = Damage;

            hittable.OnHit(new HitInfo()
            {
                DamageInfo = new DamageInfo()
                {
                    DamageAmount = damage,
                },
                Hitter = gameObject,
                
            });
        }
    }
}