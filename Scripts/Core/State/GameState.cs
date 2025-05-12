using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core
{
    public class GameState
    {
        private readonly Dictionary<string, byte[]> _loadedData = new();
        private string SavePath => Path.Combine(Application.persistentDataPath, "gameState.bin");

        public bool Dirtied = false;
   
        public void UpdateData(string id, byte[] data)
        {
            _loadedData[id] = data;
            Dirtied = true;
        }

        public byte[] GetData(string id)
        {
            if (_loadedData.IsNullOrEmpty() || !_loadedData.ContainsKey(id)) return null;
            return _loadedData[id];
        }
        
        /// <summary>
        /// Writes the entire loaded data dictionary to disk.
        /// Called periodically or on application quit.
        /// </summary>
        public void SaveData()
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            writer.Write(_loadedData.Count);
            foreach (var pair in _loadedData)
            {
                writer.Write(pair.Key);
                writer.Write(pair.Value.Length);
                writer.Write(pair.Value);
            }

            File.WriteAllBytes(SavePath, ms.ToArray());
            Debug.Log($"[GameState] Saved {_loadedData.Count} module(s) to {SavePath}");
        }
        
        /// <summary>
        /// Loads saved binary data from disk into internal cache. 
        /// Modules will pull from this data upon registration.
        /// </summary>
        public async UniTask LoadData()
        {
            if (!File.Exists(SavePath))
            {
                Debug.Log("[GameState] Save file not found, skipping load.");
                return;
            }

            var bytes = await File.ReadAllBytesAsync(SavePath);
            using var ms = new MemoryStream(bytes);
            using var reader = new BinaryReader(ms);

            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                string id = reader.ReadString();
                int len = reader.ReadInt32();
                var data = reader.ReadBytes(len);
                _loadedData[id] = data;
            }
        }

        public void ClearData(string id)
        {
            if (_loadedData.ContainsKey(id))
            {
                _loadedData.Remove(id);
                Dirtied = true;
            }
        }
    }
}
