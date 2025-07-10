using System;
using UnityEngine;

namespace Kuantech.Core
{
    /// <summary>
    /// If you make this class, check = operators for this
    /// </summary>
    [Serializable]
    public struct DamageInfo
    {
        public float DamageAmount;
    }
    
    [Serializable]
    public struct HitInfo
    {
        public GameObject Hitter;
        public DamageInfo DamageInfo;
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