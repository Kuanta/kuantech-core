using System;
using UnityEngine;

namespace Kuantech.Core.Combat
{
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
    }
    
    public interface IHittable
    {
        bool CanBeHit();
        void OnHit(GameObject attacker, DamageInfo damageInfo);
        void OnHit(HitInfo hitInfo);
    }
}