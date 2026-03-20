
[Serializable]
public class CombatVariable
{
    public float BaseValue = 0;
    public AttributeAsset AttributeAsset = null;
    public float AttributeScalar = 1;
    public virtual float GetValue(StatsModule statsModule = null)
    {
        if (statsModule == null || AttributeAsset == null) return BaseValue;
        return BaseValue +
         AttributeScalar * statsModule.GetAttributeValue(AttributeAsset);
    }

}

/// <summary>
/// Combat damage variable that considers critical chance
/// </summary>
public class CombatDamageVariable : CombatVariable
{
    public DamageType DamageType;
    public CombatVariable CriticalValue;
    public float CriticalChance;

    /// <summary>
    /// Gets damage info
    /// </summary>
    /// <param name="statsModule"></param>
    /// <returns></returns>
    public DamageInfo GetDamageInfo(StatsModule statsModule)
    {
        DamageInfo damageInfo;
        float baseValue = base.GetValue(statsModule);

        bool IsCritical = Random.Range(0, 1) < CriticalChance;
        damageInfo.IsCritical = IsCritical;

        float criticalValue = CriticalValue.GetValue(statsModule);

        //Crit multiplier can't be lower than 1
        if(criticalValue <= 1)
        {
            IsCritical = false;
        }

        if(IsCritical)
        {
            baseValue *= CriticalValue.GetValue(statsModule);
        }

        damageInfo.DamageAmount = baseValue;
        damageInfo.DamageType = DamageType;

        return damageInfo;
    }
}