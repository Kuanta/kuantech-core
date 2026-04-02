using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Kuantech.Core.Database.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.Core.Database
{
    public class KtDatabase : MonoBehaviour
    {
        [SerializeField] private string Name;
        [SerializeField] private bool UpdateFromRemote = true;
        [SerializeField] private DatabaseSheetReader SheetReader;
        public List<DataTable> Tables;
        
        private Dictionary<string, DataTable> _tablesLookup;
        
        public virtual async UniTask Initialize()
        {
            await UpdateDatabase();
            _tablesLookup = new Dictionary<string, DataTable>();
            foreach (var table in Tables)
            {
                if (_tablesLookup.ContainsKey(table.TableName))
                {
                    Debug.LogError("Duplicate table name found: " + table.TableName + ". Please ensure all table names are unique.");
                    continue;
                }
                _tablesLookup[table.TableName] = table;
                table.BuildTable();
            }
        }
        
        [Button("Update Database")]
        public async UniTask UpdateDatabase()
        {
            if (SheetReader == null || !UpdateFromRemote)
            {
                return;
            }
            await SheetReader.UpdateDatabase(this);
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
        public virtual KtDataType GetEntry(string table, string entryId, string key)
        {
            if (!_tablesLookup.ContainsKey(table))
            {
                Debug.LogError($"Table '{table}' not found in database.");
                return null;
            }

            var dbTable = _tablesLookup[table];
            if (dbTable == null) return null;
            return dbTable.GetDataEntry(entryId, key);
        }

        public DataTable GetDataTable(string table)
        {
            if (_tablesLookup.ContainsKey(table)) return _tablesLookup[table];
            return null;
        }
        
        #region Database Info

        public virtual List<DataTable> GetTables()
        {
            return Tables;
        }

        #endregion
        
        #region Reading
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
            KtDataType entry = GetEntry(table, row, column);
            
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

        public List<DataTable.RowData> Query(string tableName, Predicate<DataTable.RowData> predicate)
        {
            if (!_tablesLookup.TryGetValue(tableName, out var table))
            {
                return null;
            }
            return table.Rows.FindAll(predicate);
        }

        public DataTable.RowData FindRowData(string tableName, Predicate<DataTable.RowData> predicate)
        {
            if (!_tablesLookup.TryGetValue(tableName, out var table))
            {
                return null;
            }
            return table.Rows.Find(predicate);
        }
        #endregion

        #region Parameters Setting
        public void SetVariables(object instance, string tableName, string rowId)
        {
            var type = instance.GetType();
            var members = type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var member in members)
            {
                var attribute = member.GetCustomAttribute<KtDatabaseVariableAttribute>();
                if (attribute == null) continue;

                string column = attribute.ColumnName;

                if (member is PropertyInfo prop && prop.CanWrite)
                {
                    SetValueForMember(prop.PropertyType, value => prop.SetValue(instance, value), tableName, rowId, column);
                }
                else if (member is FieldInfo field)
                {
                    SetValueForMember(field.FieldType, value => field.SetValue(instance, value), tableName, rowId, column);
                }
            }
        }
        
        private void SetValueForMember(Type memberType, Action<object> assign, string table, string row, string column)
        {
            object result = null;

            if (memberType == typeof(int))
                result = GetInteger(table, row, column);
            else if (memberType == typeof(float))
                result = GetFloat(table, row, column);
            else if (memberType == typeof(string))
                result = GetString(table, row, column);
            else if (memberType == typeof(bool))
                result = GetBool(table, row, column);
            else if (memberType.IsEnum)
            {
                string enumString = GetString(table, row, column);
                if (Enum.TryParse(memberType, enumString, out var enumValue))
                    result = enumValue;
            }
            else
            {
                if (GetValue(table, row, column, out object value))
                    result = value;
            }

            if (result != null)
                assign(result);
        }
        #endregion
        
    }
}