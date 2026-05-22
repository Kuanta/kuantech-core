using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.Utils
{
    public class AssetLibrary : SubManager
    {
        [Serializable]
        public struct SpriteEntry
        {
            public string Id;
            public Sprite Sprite;
        }

        [Serializable]
        public struct GameObjectEntry
        {
            public string Id;
            public GameObject Prefab;
        }

        public List<SpriteEntry> SpriteEntries;
        public List<GameObjectEntry> GameObjectEntries;

        private Dictionary<string, Sprite> _idToSprite;
        private Dictionary<string, GameObject> _idToGameObject;


        public override async UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);

            foreach(var entry in SpriteEntries)
            {
                _idToSprite[entry.Id] = entry.Sprite;
            }
            foreach(var entry in GameObjectEntries)
            {
                _idToGameObject[entry.Id] = entry.Prefab;
            }
        }

        public static Sprite GetSprite(string id)
        {
            AssetLibrary context = GetContext<AssetLibrary>();
            if(context == null || !context._idToSprite.ContainsKey(id)) return null;
            return context._idToSprite[id];
        }

        public static GameObject GetGameObject(string id)
        {
            AssetLibrary context = GetContext<AssetLibrary>();
            if(context == null || !context._idToGameObject.ContainsKey(id)) return null;
            return context._idToGameObject[id];
        }
    }
}