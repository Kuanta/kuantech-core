using System;
using System.Collections.Generic;
using Kuantech.Rpg;
using UnityEngine;

namespace Kuantech.Core
{
    /// <summary>
    /// A DTO for damage info
    /// </summary>
    [Serializable]
    public struct DamageInfo
    {
        public DamageType DamageType; //Type of damge
        public float DamageAmount; //Amount of damage
        public bool IsCritical; //If is critical, useful for UI

    }
    
    [Serializable]
    public struct HitInfo
    {
        public GameObject Hitter;
        public DamageInfo DamageInfo;
        public List<DamageInfo> AdditionalDamages;
        public Vector3 HitDirection;
        public float KnockbackForce;
        public float KnockbackDuration;
    }
    
    public interface IHittable
    {
        bool CanBeHit();
        void OnHit(GameObject attacker, DamageInfo damageInfo);
        void OnHit(HitInfo hitInfo);
    }
}