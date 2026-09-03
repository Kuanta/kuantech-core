using System;
using System.Collections.Generic;
using Kuantech.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.Utils
{
    /// <summary>
    /// A simple utility class to store metadata assets. Then a metadata asset can be accessed with id
    /// </summary>
    /// <typeparam name="T">MetadataAsset type class</typeparam>
    [Serializable]
    public class MetadataAssetContainer<T> where T : MetadataAsset
    {
        public List<T> Metadatas;
        private Dictionary<string, T> _metadatasMap;

        public T GetMetadata(string id)
        {
            if(id == null || id == "") return null;
            if(_metadatasMap == null)
            {
                _metadatasMap = new Dictionary<string, T>();
                foreach(var data in Metadatas)
                {
                    _metadatasMap[data.GetId()] = data;
                }
            }

            if(_metadatasMap.ContainsKey(id)) return _metadatasMap[id];
            return null;
        }

        /// <summary>
        /// Replaces Metadatas with every T asset found anywhere in the project -- works for any closed
        /// generic instantiation (MetadataAssetContainer&lt;PerkAsset&gt;, ...&lt;EquipmentSlotType&gt;, ...)
        /// since typeof(T) resolves to the concrete type at runtime, no per-type code needed. Mirrors
        /// SkillBalancer.FindAllAssetsInProject -- same reasoning, one level more generic.
        /// </summary>
        [Button("Find All In Project")]
        public void FindAllInProject()
        {
#if UNITY_EDITOR
            Metadatas = new List<T>();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
            {
                T asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) Metadatas.Add(asset);
            }
            Metadatas.Sort((a, b) => string.CompareOrdinal(a.GetId(), b.GetId()));
            _metadatasMap = null; // stale cache -- force GetMetadata to rebuild it from the fresh list
            Debug.Log($"[MetadataAssetContainer] Found {Metadatas.Count} {typeof(T).Name} assets.");
#else
            Debug.LogWarning("[MetadataAssetContainer] FindAllInProject is editor-only.");
#endif
        }
    }
}