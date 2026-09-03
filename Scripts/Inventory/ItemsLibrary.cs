using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Inventory
{
    public class ItemsLibrary : SubManager
    {
        [Header("Item Datas")]
        public List<ItemDataAsset> ItemAssets;
        public List<ItemTemplate> ItemTemplates;

        [Header("Equipment Slots")]
        public MetadataAssetContainer<EquipmentSlotType> EquipmentSlots;
        
        private Dictionary<string, ItemData> _itemMap;
        private Dictionary<string, ItemTemplate> _templateMap;

        public override async UniTask Initialize(GameManager parentManager)
        {
            await base.Initialize(parentManager);

            _itemMap = new Dictionary<string, ItemData>();
            foreach (var asset in ItemAssets)
            {
                if (asset == null) continue;
                string id = asset.GetId();
                if (string.IsNullOrEmpty(id)) continue;
                _itemMap[id] = asset.GetItemData();
            }

            _templateMap = new Dictionary<string, ItemTemplate>();
            foreach (var template in ItemTemplates)
            {
                if (template == null || string.IsNullOrEmpty(template.TemplateId)) continue;
                _templateMap[template.TemplateId] = template;
            }
        }

        public static ItemData GetItemData(string itemId)
        {
            var ctx = GetContext<ItemsLibrary>();
            if (ctx == null || ctx._itemMap == null) return null;
            ctx._itemMap.TryGetValue(itemId, out var data);
            return data;
        }

        public static ItemTemplate GetItemTemplatePrefab(string templateId)
        {
            var ctx = GetContext<ItemsLibrary>();
            if (ctx == null || ctx._templateMap == null) return null;
            ctx._templateMap.TryGetValue(templateId, out var template);
            return template;
        }

        public static Sprite GetItemIcon(string itemId)
        {
            return GetItemData(itemId)?.GetIcon();
        }

        public static EquipmentSlotType GetEquipmentSlotById(string id)
        {
            var ctx = GetContext<ItemsLibrary>();
            if(ctx == null) return null;
            return ctx.EquipmentSlots.GetMetadata(id);
        }

        /// <summary>
        /// Adds/overwrites a synthetic ItemData built at balance-time from the database (see
        /// HordeBonkersItemsBalancer), independent of ItemAssets -- last-write-wins, same as the
        /// asset-driven entries Initialize() populates. Balancers run after Initialize() (two-phase
        /// SubManager init), so this is how a DB-driven item ends up reachable via GetItemData.
        /// </summary>
        public static void RegisterItemData(ItemData data)
        {
            var ctx = GetContext<ItemsLibrary>();
            if (ctx == null || data == null || string.IsNullOrEmpty(data.Id)) return;
            if (ctx._itemMap == null) ctx._itemMap = new Dictionary<string, ItemData>();
            ctx._itemMap[data.Id] = data;
        }
    }
}
