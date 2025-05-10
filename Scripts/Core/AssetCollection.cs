using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core
{
    [Serializable]
    public struct PrefabEntry
    {
        public IdReference PrefabId;
        public GameObject Prefab;
    }

    [Serializable]
    public struct SpriteEntry
    {
        public IdReference SpriteId;
        public Sprite Sprite;
    }
    public class AssetCollection : SubManager
    {
        [Header("Prefabs")]
        public List<PrefabEntry> PrefabEntries;
        private Dictionary<string, GameObject> _prefabs;

        [Header("Sprites")] 
        public List<SpriteEntry> SpriteEntries;
        private Dictionary<string, Sprite> _sprites;
        
        public GameObject MissingPrefab;
        

        public override async UniTask Initialize(GameManager gameManager)
        {
            _prefabs = new Dictionary<string, GameObject>();
            foreach (var entry in PrefabEntries)
            {
                _prefabs[entry.PrefabId.GetId()] = entry.Prefab;
            }

            _sprites = new Dictionary<string, Sprite>();
            foreach (var spriteEntry in SpriteEntries)
            {
                _sprites[spriteEntry.SpriteId.GetId()] = spriteEntry.Sprite;
            }
        }

        #region Prefabs
        public static GameObject GetPrefab(string id)
        {
            var ctx = GetContext<AssetCollection>();
            if (ctx == null)
            {
                Debug.LogError($"Tried to get {id} where Asset manager is null");
                return null;
            }
            if (ctx._prefabs.ContainsKey(id))
            {
                return ctx._prefabs[id];
            }
            else
            {
                Debug.LogError($"Asset Colleciton doesn't have an entry called {id}");
            }
            return null;
        }
        
        /// <summary>
        /// Gets prefab with component
        /// </summary>
        /// <param name="id"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetPrefabByType<T>(string id) where T : MonoBehaviour
        {
            var ctx = GetContext<AssetCollection>();
            if (ctx == null)
            {
                Debug.LogError($"Tried to get {id} where Asset manager is null");
                return null;
            }
            GameObject go = GetPrefab(id);
            return go.GetComponent<T>();
        }

        #endregion

        #region Sprites

        public static Sprite GetSprite(string id)
        {
            var ctx = GetContext<AssetCollection>();
            if (ctx == null)
            {
                Debug.LogError($"Tried to get {id} where Asset manager is null");
                return null;
            }

            if (ctx._sprites.ContainsKey(id)) return ctx._sprites[id];
            return null;
        }

        #endregion

    }
}