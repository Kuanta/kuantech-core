using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.Inventory
{
    public class ItemsLibrary : SubManager
    {
        public List<ItemDataAsset> ItemAssets;
        public List<ItemTemplate> ItemTemplates;

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

        public static ItemTemplate GetItemTemplate(string templateId)
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
    }
}
