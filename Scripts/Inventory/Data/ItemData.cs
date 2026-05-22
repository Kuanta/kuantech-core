using System;
using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Inventory
{
    public enum ItemRarities
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4,
    }

    [Serializable]
    public class ItemData
    {
        public string Id;
        public string Name;
        public string Description;
        public string ItemTemplateId;
        public Sprite Icon;
        public bool Stackable;
        [KTTag("ItemTag")] public int Tag;
        [SerializeReference]
        public List<ItemComponentData> Components;

        #region Metadata
        public string GetId()
        {
            return Id;
        }

        public string GetName()
        {
            return Name;
        }

        public string GetDescription()
        {
            return Description;
        }

        public Sprite GetIcon()
        {
            return Icon;
        }

        #endregion
        public T GetItemComponentData<T>() where T : ItemComponentData
        {
            if (Components== null) return null;
            foreach (var compData in Components)
                if (compData is T t) return t;
            return null;
        }
    }
}
