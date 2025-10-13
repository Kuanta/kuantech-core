using System;
using System.Collections.Generic;
using Kuantech.Rpg;
using UnityEngine;
using UnityEngine.Serialization;

namespace Kuantech.Core
{
    /// <summary>
    /// If you make this class, check = operators for this
    /// </summary>
    [Serializable]
    public struct DamageInfo
    {
        public DamageType DamageType; //Type of damge
        public float DamageAmount; //Amount of damage
        public bool IsCritical => CritMultiplier > 1; //If is critical, useful for UI

        [NonSerialized] public float CritMultiplier;
        [NonSerialized] public float AttributeValue;
        public float GetDamage()
        {
            float baseDamage = DamageAmount;
            if(DamageType != null) baseDamage += AttributeValue * DamageType.AttributeScale;
            return baseDamage * CritMultiplier;
        }

        public void SetAttributeValue(StatsModule statsModule)
        {
            if (DamageType == null || statsModule == null || DamageType.DamageScaleAttribute == null)
            {
                AttributeValue = 0;
                return;
            }

            AttributeValue = statsModule.GetAttributeValue(DamageType.DamageScaleAttribute) * DamageType.AttributeScale;
        }
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