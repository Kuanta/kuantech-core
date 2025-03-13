using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Data;
using UnityEngine;

namespace Kuantech.Rpg.Inventory
{
    public class Weapon : Item
    {
        public WeaponData WeaponData;
        public Weapon(WeaponData data) : base(data)
        {
            WeaponData = data;
        }

        public float GetDamage(int comboIndex)
        {
            //todo: Base stat???
            WeaponAttackPattern attackPattern = WeaponData.AttackPatterns[comboIndex];
            float damageAmount = attackPattern.GetDamageInfo().DamageAmount;
            float baseDamage = damageAmount * (1 + StateData.ItemLevel * WeaponData.ScalingFactor);
            if (WeaponData.BaseStat != null)
            {
                StatsModule sm = ParentInvetory.Actor.GetModule<StatsModule>();
                if (sm != null)
                {
                    baseDamage += sm.GetAttributeValue(WeaponData.BaseStat) * WeaponData.ScalingFactor;
                }
            }
            return baseDamage;
        }

        public float GetAlternativeDamage()
        {
            WeaponAttackPattern attackPattern = WeaponData.AlternativeAttackPatterns;
            if (attackPattern == null) return 0f;
            float damageAmount = attackPattern.GetDamageInfo().DamageAmount;
            float baseDamage = damageAmount * (1 + StateData.ItemLevel * WeaponData.ScalingFactor);
            if (WeaponData.BaseStat != null)
            {
                StatsModule sm = ParentInvetory.Actor.GetModule<StatsModule>();
                if (sm != null)
                {
                    baseDamage += sm.GetAttributeValue(WeaponData.BaseStat) * WeaponData.ScalingFactor;
                }
            }
            return baseDamage;
        }
    }
}