using System;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Core;
using Kuantech.Data;
using Kuantech.Utils;

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
    }

    /// <summary>
    /// Set of parameters for a single attack parameter of a weapon
    /// </summary>
    [Serializable]
    public struct WeaponAttackPattern
    {
        public AttackTypes AttackType;
        public float Damage;
        public float DamageTime;
        public float AnimationTime;
        public float Range;
        public float Angle;
        public float Width;
        public float Knockback;
        public float ProjectileSpeed;
        public float ProjectileDrop;
    }
    
    [Serializable]
    public class ItemData
    {
        // Common mandotary
        public int id;
        public string name;
        public int templateId;
        public string slotType = "None";
        public string baseStat = "None";
        public float weight;
        public bool stackable = false;
        public Enums.ItemType ItemType;

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
        public List<int> skills;
        public float blockAmount = 0; //Additional armor value
        public bool isOffHand = false;
        
    }

    [Serializable]
    public class ArmorData : ItemData
    {
        public float armorValue = 0f;
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
        public int modelPrefabId;
        public float durability;
        public Actor Owner;
        public int amount = 1;
        public int Weight = 1;
        public ItemData data;
        public ItemTemplate templateData = null;
        
        //Stats
        public ItemStateData StateData;
        
        public Item(ItemData data)
        {
            Id = data.id;
            name = data.name;
            modelPrefabId = data.templateId;
            slotType = (Enums.EquipmentSlotType)Enum.Parse(typeof(Enums.EquipmentSlotType), data.slotType);
            BaseStat = (StatTypes) Enum.Parse(typeof(StatTypes), data.baseStat);
            Type = data.ItemType;
            equipable = slotType!= Enums.EquipmentSlotType.None;
            amount = 1;
            
            this.data = data;
            if (Librarian.Instance.itemTemplates.ContainsKey(data.templateId))
            {
                templateData = Librarian.Instance.itemTemplates[data.templateId];
            }

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
        
        public ItemRarities GetItemRarity()
        {
            return StateData.ItemRarity;
        }

        public float GetSellValue()
        {
            float rarityMultiplier = 1;
            switch (StateData.ItemRarity)
            {
                case ItemRarities.Uncommon:
                    rarityMultiplier = 1.5f;
                    break;
                case ItemRarities.Rare:
                    rarityMultiplier = 2f;
                    break;
                case ItemRarities.Epic:
                    rarityMultiplier = 2.5f;
                    break;
                case ItemRarities.Legendary:
                    rarityMultiplier = 3f;
                    break;
            }
            return StateData.ItemLevel * rarityMultiplier; 
        }
     
        #endregion
        #region Modifiers

        public StatModifier GetStatModifier(StatTypes statType)
        {
            return !StateData.StatModifiers.ContainsKey(statType) ? null : StateData.StatModifiers[statType];
        }

        public void AddModifier(StatModifier modifier)
        {
            StateData.StatModifiers.Add(modifier.StatType, modifier);
        }
        
        /// <summary>
        /// Adds a random modifier from ModifierList stored in Library
        /// </summary>
        public void AddRandomModifier()
        {
            List<StatTypes> stats = Enum.GetValues(typeof(StatTypes)).Cast<StatTypes>().ToList();
            stats.Remove(StatTypes.None);
            stats.Shuffle();
            for (int i = 0; i < stats.Count; ++i)
            {
                if (!StateData.StatModifiers.ContainsKey(stats[i]))
                {
                    StatModifierData modifierData = Librarian.Instance.ModifierDataDictionary[stats[i]];
                    StatModifier newModifier = new StatModifier()
                    {
                        Level = StateData.ItemLevel,
                        StatType = modifierData.StatType,
                        BaseValue = modifierData.BaseValue,
                        ModifierType = modifierData.ModifierType,
                    };
                    AddModifier(newModifier);
                }
            }
        }
        
        /// <summary>
        /// Updates the values of modifiers according to item level
        /// </summary>
        public void UpdateModifiers()
        {
            int itemLevel = StateData.ItemLevel;
            foreach (var pair in StateData.StatModifiers)
            {
                pair.Value.Level = itemLevel;
            }
            Owner.Stats.UpdateStatModifiers();
        }
        #endregion
    }
}