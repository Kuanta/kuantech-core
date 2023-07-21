using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Data;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Serialization;

namespace Kuantech.Inventory.Items
{
    [Serializable]
    public class ItemStateData
    {
        public int ItemId;
        public int InventoryId;
        public int ItemLevel;
        public ItemRarities ItemRarity;
        public bool Equipped;
        public Dictionary<StatTypes, StatModifier> StatModifiers;
        public bool IsNew;
    }

    /// <summary>
    /// Set of parameters for a single attack parameter of a weapon
    /// </summary>
    [Serializable]
    public struct WeaponAttackPattern
    {
        public AttackTypes AttackType;
        public float Damage;
        public float MovementSlow; //Factor between 0-1, movement speed while attacking will be MovementSpeed * (1-MovementSlow)
        public float DamageTime;
        public float AnimationTime;
        public float Range;
        public float Angle;
        public float Width;
        public float Knockback;
        public float KnockbackTime;
        public float ProjectileSpeed;
        public float ProjectileDrop;
        //public float ProjectileRisHeight;
        public bool TargetedProjectile;
        public GameObject ProjectilePrefab;
    }

    [Serializable]
    public struct TemplateData
    {
        public int prefabId;
        public int iconId;
        public bool inPlace;
    }
    
    [Serializable]
    public class ItemData
    {
        // Common mandotary
        public int id;
        public string name;
        public string slotType = "None";
        public string baseStat = "None";
        public float weight;
        public float value;
        public bool stackable = false;
        public Enums.ItemType ItemType;
        public TemplateData Template;
        public int minPowerLevel;
        public int maxPowerLevel;
        
        // Icon
        public int iconId;
        public string description = "";
    }
    
    [Serializable]
    public class WeaponData : ItemData
    {
        public float damage = 1f;
        public bool ranged = false;
        public int projectilePrefabId = -1;
        public int slotSize = 1; //1 for 1 handed, >1 for two handed
        public List<WeaponAttackPattern> AttackPatterns;
        public WeaponAttackPattern alternativeAttackPattern;
        public List<int> skills;
        public float blockAmount = 0; //Additional armor value
        public bool isOffHand = false;
        public float scalingFactor = 1;
        public Enums.WeaponType weaponType;
    }

    [Serializable]
    public class ArmorData : ItemData
    {
        public float armorValue = 0f;
        public float scalingFactor = 1;
        [FormerlySerializedAs("armorType")] public Enums.ArmorType armorType;
    }
    
    [Serializable]
    public class Item
    {
        public int Id; //Id for each player
        public string name = "Item";
        public Enums.ItemType Type = Enums.ItemType.Default;
        public StatTypes BaseStat = StatTypes.None;
        public bool equipable = false;
        public bool stackable = false;
        public Enums.EquipmentSlotType slotType = Enums.EquipmentSlotType.None;
        public float durability;
        public Actor Owner;
        public int amount = 1;
        public int Weight = 1;
        public ItemData data;
        //public ItemTemplate templateData = null;
        
        //Stats
        public ItemStateData StateData;
        
        public Item(ItemData data)
        {
            Id = data.id;
            name = data.name;
            slotType = (Enums.EquipmentSlotType)Enum.Parse(typeof(Enums.EquipmentSlotType), data.slotType);
            BaseStat = (StatTypes) Enum.Parse(typeof(StatTypes), data.baseStat);
            Type = data.ItemType;
            equipable = slotType!= Enums.EquipmentSlotType.None;
            amount = 1;
            
            this.data = data;
            // if (Librarian.Instance.itemTemplates.ContainsKey(data.templateId))
            // {
            //     templateData = Librarian.Instance.itemTemplates[data.templateId];
            // }

            StateData = new ItemStateData()
            {
                ItemId = Id,
                ItemLevel = 1,
                ItemRarity = ItemRarities.Common,
                StatModifiers = new Dictionary<StatTypes, StatModifier>(),
            };
        }

        public string GetName()
        {
            string name = data.name;
            switch (StateData.ItemRarity)
            {
  
                case ItemRarities.Uncommon:
                    name = "Uncommon "+name;
                    break;
                case ItemRarities.Rare:
                    name = "Rare "+name;
                    break;
                case ItemRarities.Epic:
                    name = "Epic "+name;
                    break;
                case ItemRarities.Legendary:
                    name = "Legendary "+name;
                    break;
            }

            return name;
        }
        public static Item GetItemFromData(ItemData data)
        {
            return data.ItemType switch
            {
                Enums.ItemType.Weapon => new Weapon((WeaponData)data),
                Enums.ItemType.Armor => new Armor((ArmorData)data),
                _ => new Item(data)
            };
        }

        public void Unequip()
        {
            if (Owner == null) return;
            InventoryModule invMod = (InventoryModule) Owner.GetModuleByType(typeof(InventoryModule));
            if (invMod == null) return;
            invMod.UnequipItem(this);
        }
        
        #region States

        public void LevelUp()
        {
            AddLevel(1);
        }
        public void AddLevel(int levelsToAdd)
        {
            SetItemLevel(StateData.ItemLevel+levelsToAdd);
        }
        public void SetItemLevel(int level)
        {
            StateData.ItemLevel = level;
            UpdateModifiers();
        }

        public int GetItemLevel()
        {
            return StateData.ItemLevel;
        }
        
        public void SetItemRarity(ItemRarities rarity, bool fillModifiers=true)
        {
            StateData.ItemRarity = rarity;
            if (fillModifiers)
            {
                int currentModifierCount = StateData.StatModifiers.Count;
                int targetCount = (int) rarity - currentModifierCount;
                for (int i = 0; i < targetCount; ++i)
                {
                    AddRandomModifier();
                }
            }
        }
        
        /// <summary>
        /// Returns multiplier for item rarities. Useful in case enumeration values change
        /// </summary>
        /// <returns></returns>
        public int GetRarityCoeff()
        {
            switch (GetItemRarity())
            {
                case ItemRarities.Common:
                    return 1;
                case ItemRarities.Uncommon:
                    return 2;
                case ItemRarities.Rare:
                    return 3;
                case ItemRarities.Epic:
                    return 4;
                case ItemRarities.Legendary:
                    return 1;
                default:
                    return 1;
            }
        }
        public ItemRarities GetItemRarity()
        {
            return StateData.ItemRarity;
        }

        public int GetSellValue()
        {
            float sellValue = (StateData.ItemLevel * data.value * Config.ITEM_SELL_VALUE_COEFF) *
                              (GetRarityCoeff() * 0.5f);
            return (int)Mathf.Max(sellValue, 1); 
        }

        public int GetUpgradeValue()
        {
            return (int)(data.value*(StateData.ItemLevel + 1) + (StateData.ItemLevel + 1)*Config.ITEM_UPGRADE_COST_COEFF);
        }
        #endregion
        #region Modifiers

        public StatModifier GetStatModifier(StatTypes statType)
        {
            return !StateData.StatModifiers.ContainsKey(statType) ? null : StateData.StatModifiers[statType];
        }

        public void AddModifier(StatModifier modifier)
        {
            modifier.Level = StateData.ItemLevel;
            StateData.StatModifiers.Add(modifier.StatType, modifier);
        }
        
        /// <summary>
        /// Adds a random modifier from ModifierList stored in Library
        /// </summary>
        public void AddRandomModifier()
        {

            List<StatTypes> availableModifiers = Librarian.Instance.GetAvailableModifiers(this);
            availableModifiers.Shuffle();
            for (int i = 0; i < availableModifiers.Count; ++i)
            {
                if (StateData.StatModifiers.ContainsKey(availableModifiers[i])) continue; //Don't add same modifier twice
                StatModifierData modifierData = Librarian.Instance.ModifierDataDictionary[availableModifiers[i]];
                StatModifier newModifier = new StatModifier()
                {
                    Level = StateData.ItemLevel,
                    StatType = modifierData.StatType,
                    BaseValue = modifierData.BaseValue,
                    ModifierType = modifierData.ModifierType,
                    LevelToValueFactor = modifierData.LevelToValueFactor,
                };
                AddModifier(newModifier);
                return;
            }
        }
        
        /// <summary>
        /// Updates the values of modifiers according to item level
        /// </summary>
        public void UpdateModifiers()
        {
            int itemLevel = StateData.ItemLevel;
            if (StateData.StatModifiers == null) return;
            foreach (var pair in StateData.StatModifiers)
            {
                pair.Value.Level = itemLevel;
            }
            Owner.Stats.UpdateStatModifiers();
        }
        #endregion
    }
}