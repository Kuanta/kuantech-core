using System;
using System.Collections.Generic;
using Kuantech.Core;
using UnityEngine.Serialization;

namespace Kuantech.Rpg.Inventory
{
    [Serializable]
    public class WeaponData : ItemData
    {
        [FormerlySerializedAs("BaseStat")] public AttributeAsset @base;
        public bool Ranged = false;
        public Projectile ProjectilePrefab;
        public int SlotSize = 1; //1 for 1 handed, >1 for two handed
        public List<WeaponAttackPattern> AttackPatterns;
        public WeaponAttackPattern AlternativeAttackPatterns;
        public List<int> Skills;
        public float blockAmount = 0; //Additional armor value
        public float ScalingFactor = 1;
    }
}