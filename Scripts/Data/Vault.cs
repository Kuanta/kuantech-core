using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kuantech.Utils;
using SQLite4Unity3d;

namespace Kuantech.Core.Data
{
    /// <summary>
    /// Vault is a class that stores id based datas
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class Vault<T> where T : VaultData
    {
        public List<T> DataList;
        private Dictionary<string, T> _dataMap;
        
        /// <summary>
        /// Initializes the vault
        /// </summary>
        protected virtual void Initialize()
        {
            if (DataList.IsNullOrEmpty()) return;
            _dataMap = new Dictionary<string, T>();
            foreach (var data in DataList)
            {
                _dataMap[data.Id] = data;
            }
        }
        
        #region Data Loading Methods
        /// <summary>
        /// Loads the data
        /// </summary>
        public async virtual UniTask LoadDataFromList(List<T> datas)
        {
            DataList = datas;
            Initialize();
        }

        public async virtual UniTask LoadFromDatabase(SQLiteConnection sqLiteConnection, string tableName)
        {
            //Throw non implemented error
        }
        #endregion

        
        /// <summary>
        /// Returns data by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public T GetDataById(string id)
        {
            if (_dataMap.ContainsKey(id)) return _dataMap[id];
            return null;
        }
    }
}