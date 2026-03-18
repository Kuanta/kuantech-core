using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Kuantech.Core
{
    [Serializable]
    public class BinaryStorageProvider : DataStorageProvider
    {
        [SerializeField] private string _id;
        [SerializeField] private string _fileName = "gameState.bin";

        public override string Id => _id;
        public override bool HasUnsavedChanges => _dirty;

        private readonly Dictionary<string, byte[]> _cache = new();
        private bool _dirty;

        private string SavePath => Path.Combine(Application.persistentDataPath, _fileName);

        /// <summary>
        /// Reads the binary file from disk into the in-memory cache.
        /// </summary>
        public override void LoadData()
        {
            _cache.Clear();
            _dirty = false;

            if (!File.Exists(SavePath))
            {
                Debug.Log($"[BinaryStorageProvider:{_id}] No save file found at {SavePath}, starting fresh.");
                return;
            }

            try
            {
                var bytes = File.ReadAllBytes(SavePath);
                using var ms = new MemoryStream(bytes);
                using var reader = new BinaryReader(ms);

                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    string key = reader.ReadString();
                    int len = reader.ReadInt32();
                    _cache[key] = reader.ReadBytes(len);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[BinaryStorageProvider:{_id}] Failed to load from {SavePath}: {e.Message}");
            }
        }

        /// <summary>
        /// Writes the in-memory cache to disk if there are unsaved changes.
        /// </summary>
        public override void SaveChanges()
        {
            try
            {
                using var ms = new MemoryStream();
                using var writer = new BinaryWriter(ms);

                writer.Write(_cache.Count);
                foreach (var pair in _cache)
                {
                    writer.Write(pair.Key);
                    writer.Write(pair.Value.Length);
                    writer.Write(pair.Value);
                }

                File.WriteAllBytes(SavePath, ms.ToArray());
                _dirty = false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[BinaryStorageProvider:{_id}] Failed to save to {SavePath}: {e.Message}");
            }
        }

        public override void Clear()
        {
            _cache.Clear();
            _dirty = true;
        }

        // --- ISaveable helpers ---

        /// <summary>
        /// Serializes the saveable and stores it in the cache.
        /// </summary>
        public void SaveData(ISaveable saveable)
        {
            _cache[GetKey(saveable)] = SaveUtility.Serialize(saveable);
            _dirty = true;
        }

        /// <summary>
        /// Deserializes cached data into the saveable. Returns false if no data exists.
        /// </summary>
        public bool LoadData(ISaveable saveable)
        {
            if (!_cache.TryGetValue(GetKey(saveable), out var bytes)) return false;
            SaveUtility.Deserialize(bytes, saveable);
            return true;
        }

        /// <summary>
        /// Removes the cached entry for this saveable.
        /// </summary>
        public void ClearData(ISaveable saveable) => ClearData(GetKey(saveable));

        // --- Raw key/value helpers (for POCO objects) ---

        public void SaveRaw(string key, byte[] data)
        {
            _cache[key] = data;
            _dirty = true;
        }

        public bool TryLoadRaw(string key, out byte[] data) => _cache.TryGetValue(key, out data);

        public void ClearData(string key)
        {
            if (_cache.Remove(key))
                _dirty = true;
        }

        private static string GetKey(ISaveable saveable) => saveable.GetType().FullName;
    }
}
