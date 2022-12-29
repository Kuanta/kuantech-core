using System;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.Core.Utils
{
    public class DamageDealer : MonoBehaviour
    {
        public SphereCollider SphereCollider;
        public EventHandler<Actor> DamageEvent;
        private float _damage;
        private float _range;
        private float _knockback;
        private float _angle;
        private CombatModule _combatModule;
        public float Frequency;
        private float _lastAttackTime;
        
        public void Initialize(CombatModule combatModule, float damage, float range, float knockback, float angle)
        {
            if (combatModule == null) return;
            _combatModule = combatModule;
            SphereCollider.radius = range;
            _range = range;
            _damage = damage;
            _knockback = knockback;
            _angle = angle;
            _lastAttackTime = Time.time;

        }
        private void Update()
        {
            if (_combatModule == null) return;
            if (Time.time - _lastAttackTime >= Frequency)
            {
                CombatModule.DealAreaDamage(_combatModule, _damage, _range, _knockback, false,false, _angle);
                _lastAttackTime = Time.time;
            }
        }
        private void OnTriggerEnter(Collider collider)
        {
            if (_combatModule == null) return;
            if (collider.TryGetComponent(out Actor actor) && actor != _combatModule.Actor)
            {
                actor.ReceiveDamage(_combatModule.Actor, _damage);
            }
        }
        
    }
}