using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.Combat;
using Kuantech.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Kuantech.Rpg.Inventory
{
    [Serializable]
    public enum ItemType
    {
        Default = -1,
        Weapon,
        Armor,
        Trinket,
        Consumable,
    }
    
    [Serializable]
    public class ItemStateData
    {
        public string ItemId;
        public int InventoryId;
        public int ItemLevel;
        public ItemRarities ItemRarity;
        public bool Equipped;
        public Dictionary<StatAttributeAsset, StatModifier> StatModifiers;
        public bool IsNew;
    }

    /// <summary>
    /// Set of parameters for a single attack parameter of a weapon
    /// </summary>
    [Serializable]
    public class WeaponAttackPattern
    {
        public AttackTypes AttackType;
        public DamageInfo DamageInfo;
        public float MovementSlow; //Factor between 0-1, movement speed while attacking will be MovementSpeed * (1-MovementSlow)
        public float DamageTime;
        public float AnimationTime;
        public float WindupTime;
        public float Cooldown;
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

        public DamageInfo GetDamageInfo()
        {
            return DamageInfo;
        }
    }




    
    [Serializable]
    public class Item
    {
        public InventoryModule ParentInvetory;
        public int Amount = 1;
        public ItemData Data;
        
        //Stats
        public ItemStateData StateData;
        
        //Runtime
        public float CurrentDurability;
        public EquipmentSlotType CurrentSlot;
        
        public Item(ItemData data)
        {
            
            Amount = 1;
            Data = data;
        }

        #region Equip

        public bool IsEquipable()
        {
            return !Data.SuitableSlots.IsNullOrEmpty(); //Check none?
        }

        public bool CanBeEquippedToSlot(EquipmentSlotType slotType)
        {
            if (!IsEquipable()) return false;
            return Data.SuitableSlots.Contains(slotType);
        }

        public EquipmentSlotType[] GetOccupyingSlots(EquipmentSlotType equippedSlotType)
        {
            if (Data.OccupiedSlots.IsNullOrEmpty())
            {
                return new EquipmentSlotType[]
                {
                    equippedSlotType
                };
            }

            return Data.OccupiedSlots.ToArray();
        }
        #endregion

        #region Getters
        /// <summary>
        /// Returns the item id
        /// </summary>
        /// <returns></returns>
        public string GetId()
        {
            return Data.Id;
        }
        
        /// <summary>
        /// Returns the item name
        /// </summary>
        /// <returns></returns>
        public string GetName()
        {
            string name = Data.Name;
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

        #endregion
       
        public static Item GetItemFromData(ItemData data)
        {
            return data.ItemType switch
            {
                ItemType.Weapon => new Weapon((WeaponData)data),
                ItemType.Armor => new Armor((ArmorData)data),
                _ => new Item(data)
            };
        }

        public void Unequip()
        {
            if (ParentInvetory== null) return;
            ParentInvetory.UnequipItem(this);
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
            float sellValue = (StateData.ItemLevel * Data.value * RpgConfig.ITEM_SELL_VALUE_COEFF) *
                              (GetRarityCoeff() * 0.5f);
            return (int)Mathf.Max(sellValue, 1); 
        }

        public int GetUpgradeValue()
        {
            return (int)(Data.value*(StateData.ItemLevel + 1) + (StateData.ItemLevel + 1)*RpgConfig.ITEM_UPGRADE_COST_COEFF);
        }
        #endregion
        #region Modifiers

        public StatModifier GetStatModifier(StatAttributeAsset statType)
        {
            return !StateData.StatModifiers.ContainsKey(statType) ? null : StateData.StatModifiers[statType];
        }

        public void AddModifier(StatModifier modifier)
        {
            modifier.Level = StateData.ItemLevel;
            StateData.StatModifiers.Add(modifier.AttributeAsset, modifier);
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

            StatsModule sm = ParentInvetory.Actor.GetModule<StatsModule>();
            if (sm != null)
            {
                sm.UpdateStatModifiers();
            }
        }
        #endregion

        #region Visuals

        public ItemVisual SpawnItemVisual()
        {
            ItemVisual itemVisualPrefab = AssetCollection.GetPrefabByType<ItemVisual>(Data.ItemTemplateId);
            if (itemVisualPrefab == null) return null;
            return PoolManager.GetObjectFromPool(itemVisualPrefab.gameObject).GetComponent<ItemVisual>();
        }

        #endregion
    }
}