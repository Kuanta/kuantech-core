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
        public bool Ranged = false;
        public bool IsOffHand = false;
        public int SlotSize = 1;
        public GameObject ProjectilePrefab = null;
        public List<WeaponAttackPattern> AttackPatterns;
        public List<int> SupportedSkills;
        public EquipmentModel EquipmentModel;

        public Weapon(WeaponData data) : base(data)
        {
            AttackPatterns = data.AttackPatterns;
            Ranged = data.ranged;
            Damage = data.damage;
            IsOffHand = data.isOffHand;
            ProjectilePrefab = Librarian.Instance.GetProjectilePrefab(data.projectilePrefabId);
            SupportedSkills = data.skills;
            Type = Enums.ItemType.Weapon;
        }

        public float GetDamage(int comboIndex)
        {
            float baseStat;
            if (Owner != null)
            {
                baseStat = Owner.Stats.GetStat(BaseStat);
            }
            else
            {
                baseStat = Damage;
            }
            return Damage + StateData.ItemLevel + baseStat; //todo(Design): Re-design the final damages
        }
    }
}