using System.Collections.Generic;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.Inventory
{
    [CreateAssetMenu(fileName = "ItemData", menuName = "Kuantech/Inventory/ItemData")]
    public class ItemDataAsset : MetadataAsset
    {
        public float weight;
        public float value;
        public bool stackable;

        public string ItemTemplateId;
        public string IconId;

        [SerializeReference]
        public List<ItemComponentData> Components;

        public T GetDefinition<T>() where T : ItemComponentData
        {
            if (Components == null) return null;
            foreach (var comp in Components)
                if (comp is T t) return t;
            return null;
        }
    }
}
