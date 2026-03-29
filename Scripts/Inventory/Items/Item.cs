using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Rpg;
using Kuantech.Utils;

namespace Kuantech.Inventory
{
    [Serializable]
    public class ItemStateData
    {
        public string ItemId;
        public int InventoryId;
        public int ItemLevel;
        public bool Equipped;
        public Dictionary<AttributeAsset, StatModifier> StatModifiers;
    }

    [Serializable]
    public class Item
    {
        public InventoryModule ParentInvetory;
        public int Amount = 1;
        public ItemData Data;
        
        //Stats
        public ItemStateData StateData;
        public EquipmentSlotType CurrentSlot;
        [NonSerialized] public ItemVisual ItemVisual;

        //Comps
        private Dictionary<Type, ItemComponent> _components;
        
        public Item(ItemData data)
        {
            Amount = 1;
            Data = data;
            StateData = new ItemStateData
            {
                StatModifiers = new Dictionary<AttributeAsset, StatModifier>()
            };

            _components = new Dictionary<Type, ItemComponent>();
            if(Data.Components == null) return;
            foreach(var component in Data.Components)
            {
                _components[component.GetType()] = component; //Is this polymorphic?
            }
        }

        #region Item Data Components
        public T GetItemComponent<T>() where T : ItemComponent
        {
            if (_components.TryGetValue(typeof(T), out ItemComponent component))
                return component as T;
            return null;
        }

        public bool HasItemComponent<T>() where T : ItemComponent
        {
            return _components.ContainsKey(typeof(T));
        }
        #endregion

        #region Checks
        public bool CanEquip(EquipmentSlotType slotType)
        {
            if(_components.IsNullOrEmpty()) return true;
            foreach(var pair in _components)
            {
                if(!pair.Value.CanEquipItem(this, slotType)) return false;
            }
            return true;
        }

        public bool CanUnequip()
        {
            if (_components.IsNullOrEmpty()) return true;
            foreach (var pair in _components)
            {
                if (!pair.Value.CanUnequipItem(this)) return false;
            }
            return true;
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
            return name;
        }

        public int GetInventoryId()
        {
            return StateData.InventoryId;
        }

        public string GetItemId()
        {
            return Data.Id;
        }
        #endregion
       
        public static Item GetItemFromData(ItemData data)
        {
            return new Item(data);
        }

        #region Operations
        /// <summary>
        /// Called when item added
        /// </summary>
        public void OnAdded()
        {
            foreach(var comp in _components.Values)
            {
                comp.OnItemAdded(this);
            }
        }

        /// <summary>
        /// Called when item removed
        /// </summary>
        public void OnRemoved()
        {
            foreach (var comp in _components.Values)
            {
                comp.OnItemRemoved(this);
            }
        }

        /// <summary>
        /// Equips the item
        /// </summary>
        public bool Equip(EquipmentSlotType slotType=null)
        {
            if(!CanEquip(slotType)) return false;

            foreach (var comp in _components.Values)
            {
                comp.OnItemEquipped(this, slotType);
            }
            return true;
        }

        /// <summary>
        /// Unequips the item
        /// </summary>
        public bool Unequip()
        {
            if(!CanUnequip()) return false;

            foreach (var comp in _components.Values)
            {
                comp.OnItemUnequipped(this);
            }
            return true;
        }

        /// <summary>
        /// Uses the item
        /// </summary>
        public void Use()
        {
            foreach (var comp in _components.Values)
            {
                comp.OnItemUsed(this);
            }
        }
        #endregion

        #region Visuals

        public ItemVisual SpawnItemVisual()
        {
            return SpawnItemVisual(Data.ItemTemplateId);
        }

        public static ItemVisual SpawnItemVisual(string itemVisualId)
        {
            ItemVisual itemVisualPrefab = AssetCollection.GetPrefabByType<ItemVisual>(itemVisualId);
            if (itemVisualPrefab == null) return null;
            return PoolManager.GetObjectFromPool(itemVisualPrefab.gameObject).GetComponent<ItemVisual>();
        }

        #endregion
    }
}