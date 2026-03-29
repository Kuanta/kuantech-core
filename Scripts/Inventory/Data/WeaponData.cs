using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Rpg;
using UnityEngine.Serialization;

namespace Kuantech.Inventory
{
    [Serializable]
    public class WeaponData : ItemData
    {
        [FormerlySerializedAs("BaseStat")] public AttributeAsset @base;
        public bool Ranged = false;
        [FormerlySerializedAs("oldProjectilePrefab")] [FormerlySerializedAs("ProjectilePrefab")] public Projectile projectilePrefab;
        public int SlotSize = 1; //1 for 1 handed, >1 for two handed
        public List<AttackPattern> AttackPatterns;
        public AttackPattern AlternativeAttackPatterns;
        public List<int> Skills;
        public float blockAmount = 0; //Additional armor value
        public float ScalingFactor = 1;
    }
}