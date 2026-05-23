using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.Inventory
{
    public class ItemsLibrary : SubManager
    {
        public List<ItemData> ItemDatas;
        public List<ItemDataAsset> ItemAssets;
        private Dictionary<string, ItemData> _assetMap;

        public override async UniTask Initialize(GameManager parentManager)
        {
            await base.Initialize(parentManager);
            _assetMap = new Dictionary<string, ItemData>();
            foreach (var asset in ItemAssets)
            {
                if (asset == null) continue;
                string id = asset.GetId();
                if (string.IsNullOrEmpty(id)) continue;
                _assetMap[id] = asset.GetItemData();
            }
        }

        public static ItemData GetItemData(string itemId)
        {
            var ctx = GetContext<ItemsLibrary>();
            if (ctx?._assetMap == null) return null;
            ctx._assetMap.TryGetValue(itemId, out var asset);
            return asset;
        }

        public static Sprite GetItemIcon(string itemId)
        {
            return GetItemData(itemId)?.GetIcon();
        }
    }
}
