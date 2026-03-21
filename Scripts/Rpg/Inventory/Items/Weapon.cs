using Kuantech.Core;


namespace Kuantech.Rpg.Inventory
{
    public class Weapon : Item
    {
        public WeaponData WeaponData;
        public Weapon(WeaponData data) : base(data)
        {
            WeaponData = data;
        }

        public float GetDamage(int comboIndex, StatsModule statsModule)
        {
            //todo: Base stat???
            AttackPattern attackPattern = WeaponData.AttackPatterns[comboIndex];
            float damageAmount = attackPattern.Damage.GetDamageInfo(statsModule).GetDamage();
            float baseDamage = damageAmount * (1 + StateData.ItemLevel * WeaponData.ScalingFactor);
            if (WeaponData.@base != null)
            {
                StatsModule sm = ParentInvetory.Actor.GetModule<StatsModule>();
                if (sm != null)
                {
                    baseDamage += sm.GetAttributeValue(WeaponData.@base) * WeaponData.ScalingFactor;
                }
            }
            return baseDamage;
        }

        public float GetAlternativeDamage(StatsModule statsModule)
        {
            AttackPattern attackPattern = WeaponData.AlternativeAttackPatterns;
            if (attackPattern == null) return 0f;
            float damageAmount = attackPattern.Damage.GetDamageInfo(statsModule).GetDamage();
            float baseDamage = damageAmount * (1 + StateData.ItemLevel * WeaponData.ScalingFactor);
            if (WeaponData.@base != null)
            {
                StatsModule sm = ParentInvetory.Actor.GetModule<StatsModule>();
                if (sm != null)
                {
                    baseDamage += sm.GetAttributeValue(WeaponData.@base) * WeaponData.ScalingFactor;
                }
            }
            return baseDamage;
        }
    }
}