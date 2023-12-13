using System;
using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeData", menuName = "Kuantech/Upgrades", order = 0)]
public class UpgradeData : ScriptableObject 
{
    public string UpgradeName;
    public string UpgradeId;
    public Sprite UpgradeIcon;
    public int MaxLevel;
    public LeveledValueInt UpgradePrice;
    public CurrencyData CurrencyData;
    public LeveledValueFloat Value;
    [NonSerialized] public int CurrentLevel;

    public float GetValue()
    {
        return Value.GetValue(CurrentLevel);
    }

    public int GetUpgradePrice()
    {
        return UpgradePrice.GetValue(CurrentLevel);
    }

    public void SetLevel(int level)
    {
        CurrentLevel = level;
    }
}