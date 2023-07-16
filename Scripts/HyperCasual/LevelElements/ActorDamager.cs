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
            if (!other.TryGetComponent(out Actor actor)) return;
            if (actor.FactionId == FactionId) return;
            DealDamage(actor);
        }

        protected virtual void DealDamage(Actor actor)
        {
            float damage = 0;
            if (PercentageDamage)
            {
                damage = actor.Stats.GetStat(StatTypes.MaxHealth) * Mathf.Clamp01(Damage);
            }
            else
            {
                damage = Damage;
            }

            actor.ReceiveDamage(null, damage, RawDamage);
        }
    }
}