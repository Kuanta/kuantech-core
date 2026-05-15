using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.Inventory
{
    public class ItemsManager : SubManager
    {
        public List<ItemDataAsset> ItemAssets;
        private Dictionary<string, ItemDataAsset> _assetMap;

        public override async UniTask Initialize(GameManager parentManager)
        {
            await base.Initialize(parentManager);
            _assetMap = new Dictionary<string, ItemDataAsset>();
            foreach (var asset in ItemAssets)
            {
                if (asset == null) continue;
                string id = asset.GetId();
                if (string.IsNullOrEmpty(id)) continue;
                if (asset.ItemData != null) asset.ItemData.Id = id;
                _assetMap[id] = asset;
            }
        }

        public static ItemData GetItemData(string itemId)
        {
            var ctx = GetContext<ItemsManager>();
            if (ctx?._assetMap == null) return null;
            ctx._assetMap.TryGetValue(itemId, out var asset);
            return asset?.ItemData;
        }

        public static ItemDataAsset GetItemAsset(string itemId)
        {
            var ctx = GetContext<ItemsManager>();
            if (ctx?._assetMap == null) return null;
            ctx._assetMap.TryGetValue(itemId, out var asset);
            return asset;
        }

        public static Sprite GetItemIcon(string itemId)
        {
            return GetItemAsset(itemId)?.GetIcon();
        }
    }
}
