using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Data;
using UnityEngine;

namespace Kuantech.Inventory.Items
{
    public class Weapon : Item
    {
        public Enums.WeaponType WeaponType = Enums.WeaponType.OneHanded;
        public float Damage = 1;
        public bool Ranged = false; //If Ranged, primary attack patterns are RangedAttackPatters
        public bool IsOffHand = false;
        public int SlotSize = 1;
        public GameObject ProjectilePrefab = null;
        public List<WeaponAttackPattern> AttackPatterns;
        public WeaponAttackPattern AlternativeAttackPattern; //For ranged weapons
        public List<int> SupportedSkills;
        public EquipmentModel EquipmentModel;
        public float ScalingFactor;
        public float StatScalingFactor = 0.2f;
        public Weapon(WeaponData data) : base(data)
        {
            AttackPatterns = data.AttackPatterns;
            AlternativeAttackPattern = data.alternativeAttackPattern;
            Ranged = data.ranged;
            Damage = data.damage;
            IsOffHand = data.isOffHand;
            ProjectilePrefab = Librarian.Instance.GetProjectilePrefab(data.projectilePrefabId);
            SupportedSkills = data.skills;
            Type = Enums.ItemType.Weapon;
            ScalingFactor = data.scalingFactor;
            WeaponType = data.weaponType;
        }

        public float GetDamage(int comboIndex)
        {
            //todo: Base stat???
            float baseDamage = Damage * (1 + StateData.ItemLevel * ScalingFactor);
            if (BaseStat != StatTypes.None)
            {
                baseDamage += Owner.Stats.GetStat(BaseStat) * StatScalingFactor;
            }
            return baseDamage;
        }

        public float GetAlternativeDamage()
        {
            float baseDamage = AlternativeAttackPattern.Damage * (1 + StateData.ItemLevel * ScalingFactor);
            if (BaseStat != StatTypes.None)
            {
                baseDamage += Owner.Stats.GetStat(BaseStat) * StatScalingFactor;
            }
            return baseDamage;
        }
    }
}