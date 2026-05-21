using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core
{
    public class PrefabLibrary : SubManager
    {
        [Serializable]
        public struct PrefabLibraryEntry
        {
            public string PrefabEntryName;
            public GameObject Prefab;
        }
        
        [Serializable]
        public struct PrefabLibraryData
        {
            public string Category;
            public PrefabLibraryEntry[] Entries;
        }
        
        [Header("Prefabs")]
        public List<PrefabLibraryData> PrefabLibraryDataList;
        
        [Header("Default")]
        public string DefaultCategory = "Default";
        
        private Dictionary<string, List<GameObject>> _prefabLibrary;
        private Dictionary<string, GameObject> _prefabsById;
        
        public override async UniTask Initialize(GameManager gameManager)
        {
            _prefabLibrary = new Dictionary<string, List<GameObject>>();
            _prefabsById = new Dictionary<string, GameObject>();
            foreach (var data in PrefabLibraryDataList)
            {
                if (!_prefabLibrary.ContainsKey(data.Category))
                {
                    _prefabLibrary[data.Category] = new List<GameObject>();
                }

                foreach (var entry in data.Entries)
                {
                    _prefabLibrary[data.Category].Add(entry.Prefab);
                    if (!string.IsNullOrEmpty(entry.PrefabEntryName) && !_prefabsById.ContainsKey(entry.PrefabEntryName))
                    {
                        _prefabsById[entry.PrefabEntryName] = entry.Prefab;
                    }
                }
            }
        }
        
        public List<GameObject> GetPrefabsByCategory(string categoryName)
        {
            if (_prefabLibrary.TryGetValue(categoryName, out var prefabs))
            {
                return prefabs;
            }
            return null;
        }
        
        /// <summary>
        /// Returns prefab from default category
        /// </summary>
        /// <param name="prefabIndex"></param>
        /// <returns></returns>
        public static GameObject GetPrefab(int prefabIndex)
        {
            var ctx = GetContext<PrefabLibrary>();
            if (ctx == null) return null;
            List<GameObject> prefabs = ctx.GetPrefabsByCategory(ctx.DefaultCategory);
            if (prefabs == null) return null;
            if (prefabs.IsValidIndex(prefabIndex))
            {
                return prefabs[prefabIndex];
            }
            return null;
        }
        
        /// <summary>
        /// Returns prefab from specified category by index
        /// </summary>
        /// <param name="categoryName"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static GameObject GetPrefab(string categoryName, int index)
        {
            var ctx = GetContext<PrefabLibrary>();
            if (ctx == null) return null;
            List<GameObject> prefabs = ctx.GetPrefabsByCategory(categoryName);
            if (prefabs.IsValidIndex(index))
            {
                return prefabs[index];
            }

            return null;
        }
        
        /// <summary>
        /// Returns prefab by id
        /// </summary>
        /// <param name="prefabId"></param>
        /// <returns></returns>
        public static GameObject GetPrefabById(string prefabId)
        {
            var ctx = GetContext<PrefabLibrary>();
            if (ctx == null) return null;
            if (ctx._prefabsById == null) return null;
            if (ctx._prefabsById.ContainsKey(prefabId))
            {
                return ctx._prefabsById[prefabId];
            }

            return null;
        }
    }
}