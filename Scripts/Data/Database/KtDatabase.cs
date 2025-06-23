using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Kuantech.Core.Database
{
    public abstract class KtDatabase : MonoBehaviour
    {
        [SerializeField] private string Name;
        
        public virtual async UniTask Initialize()
        {
        }
        
        public string GetDbName()
        {
            return Name;
        }
        /// <summary>
        /// Returns data entry for the specified table, row, and column.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="entryId">Entry id</param>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual KtDataEntry GetEntry(string table, string entryId, string key)
        {
            throw new NotImplementedException();
        }

        #region Value Getters
        /// <summary>
        /// Returns the value of the specified type for the given table, row, and column.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual bool GetValue<T>(string table, string row, string column, out T value)
        {
            KtDataEntry entry = GetEntry(table, row, column);
            
            //Set default value for value
            value = default;
            if (entry == null) return false;
            value = entry.Get<T>();
            return true;
        }
        
        public float GetFloat(string table, string row, string column, float defaultValue = 0f)
        {
            if (GetValue(table, row, column, out float value))
            {
                return value;
            }
            return defaultValue;
        }
        
        public string GetString(string table, string row, string column, string defaultValue = "")
        {
            if (GetValue(table, row, column, out string value))
            {
                return value;
            }
            return defaultValue;
        }

        public int GetInteger(string table, string row, string column, int defaultValue = 0)
        {
            if (GetValue(table, row, column, out int value))
            {
                return value;
            }

            return defaultValue;
        }

        public bool GetBool(string table, string row, string column, bool defaultValue = false)
        {
            if (GetValue(table, row, column, out bool value))
            {
                return value;
            }

            return defaultValue;
        }
        #endregion

    }
}