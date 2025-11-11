using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core
{
    /// <summary>
    /// Class that represents the game state
    /// </summary>
    public class GameState
    {
        private readonly Dictionary<string, byte[]> _loadedData = new();
        private string SavePath => Path.Combine(Application.persistentDataPath, "gameState.bin"); //todo: Don't hardcode the save file name

        public bool Dirtied = false;
        
        #region CRUD
        /// <summary>
        /// Gets data with given id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public byte[] GetData(string id)
        {
            if (_loadedData.IsNullOrEmpty() || !_loadedData.ContainsKey(id)) return null;
            return _loadedData[id];
        }
        
        /// <summary>
        /// Updates/Creates data with given id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="data"></param>
        public void UpdateData(string id, byte[] data)
        {
            _loadedData[id] = data;
            Dirtied = true;
        }

        /// <summary>
        /// Clears data with the given id
        /// </summary>
        /// <param name="id"></param>
        public void ClearData(string id)
        {
            if (_loadedData.ContainsKey(id))
            {
                _loadedData.Remove(id);
                Dirtied = true;
            }
        }
        
        /// <summary>
        /// Clears all data
        /// </summary>
        public void ClearAllData()
        {
            _loadedData.Clear();
            Dirtied = true;
        }
        #endregion
        

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
        }
        
        /// <summary>
        /// Loads saved binary data from disk into internal cache. 
        /// Modules will pull from this data upon registration.
        /// </summary>
        public async UniTask LoadData()
        {
            try
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
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
