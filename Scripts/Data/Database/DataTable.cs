using System;
using System.Collections.Generic;
using System.Reflection;
using Kuantech.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.Core.Database
{
    [CreateAssetMenu(fileName = "DataTable", menuName = "Kuantech/Data/DataTable")]
    public class DataTable : ScriptableObject
    {
        public string TableName;
        
        public List<ColumnSchema> Schema = new();
        
        public List<RowData> Rows = new();
        
        private Dictionary<string, RowData> _rowLookup;
        
        [Serializable]
        public class ColumnSchema
        {
            public string Name;
            [SerializeReference]
            public KtDataType Data;
        }
        
        [Serializable]
        public class CellData
        {
            public string Key;
            [SerializeReference]
            public KtDataType Value;
        }
        
        [Serializable]
        public class RowData
        {
            public string Id;
            [SerializeReference]
            public List<CellData> Values;

            public CellData GetCellData(string key)
            {
                int columnIndex = Values.FindIndex(c => c.Key == key);
                if (columnIndex < 0)
                {
                    return null;
                }
                return Values[columnIndex];
            }
            
            public T GetValue<T>(string key)
            {
                CellData cellData = GetCellData(key);
                if (cellData == null) return default;
                return cellData.Value.Get<T>();
            }

            public float GetFloatValue(string key, float defaultValue)
            {
                CellData cellData = GetCellData(key);
                if (cellData == null) return defaultValue;
                try
                {
                    return cellData.Value.Get<float>();
                }
                catch (Exception e)
                {
                    return defaultValue;
                }
            }
            
            public string GetStringValue(string key, string defaultValue)
            {
                CellData cellData = GetCellData(key);
                if (cellData == null) return defaultValue;
                try
                {
                    return cellData.Value.Get<string>();
                }
                catch (Exception e)
                {
                    return defaultValue;
                }
            }
            
            public int GetIntValue(string key, int defaultValue)
            {
                CellData cellData = GetCellData(key);
                if (cellData == null) return defaultValue;
                try
                {
                    return cellData.Value.Get<int>();
                }
                catch (Exception e)
                {
                    return defaultValue;
                }
            }
        }


        public void BuildTable()
        {
            _rowLookup = new Dictionary<string, RowData>();

            foreach (var row in Rows)
            {
                if (!string.IsNullOrWhiteSpace(row.Id))
                {
                    _rowLookup[row.Id] = row;
                }
            }
        }

        
        /// <summary>
        /// Clones a kt data
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        private static KtDataType CloneKtData(KtDataType original)
        {
            return original.Clone();
        }

        #region Reading
        public RowData GetRow(string rowId)
        {
            if (_rowLookup.IsNullOrEmpty() || !_rowLookup.ContainsKey(rowId)) return null;
            return _rowLookup[rowId];
        }
        
        public KtDataType GetDataEntry(string rowId, string entryKey)
        {
            RowData rowData = GetRow(rowId);
            if (rowData == null) return null;
            CellData cellData = rowData.GetCellData(entryKey);
            if (cellData == null) return null;
            return cellData.Value;
        }
        
        public T GetValue<T>(string rowId, string entryKey)
        {
            KtDataType entry = GetDataEntry(rowId, entryKey);
            if (entry == null) return default;
            return entry.Get<T>();
        }
        #endregion

        #region Write
                
        [Button("Add New Row")]
        public RowData AddNewRow(string rowId)
        {
            var row = new RowData {Id = rowId};
            row.Values = new List<CellData>();
            foreach (var column in Schema)
            {
                CellData cellData = new CellData();
                cellData.Key = column.Name;
                cellData.Value = CloneKtData(column.Data);
                row.Values.Add(cellData);
            }

            Rows.Add(row);
            if (_rowLookup != null)
            {
                _rowLookup.Add(rowId, row);
            }
            return row;
        }
        #endregion

        #region Data Setting
        /// <summary>
        /// Given an object, set KtDatabaseVariables from RowData
        /// </summary>
        public static void SetVariablesFromRow(object instance, RowData row)
        {
            if (instance == null || row == null) return;

            var type = instance.GetType();
            var members = type.GetMembers(System.Reflection.BindingFlags.Public |
                                          System.Reflection.BindingFlags.NonPublic |
                                          System.Reflection.BindingFlags.Instance);

            foreach (var member in members)
            {
                // Detect Ktdatabasevariables
                var attribute = member.GetCustomAttribute<Attributes.KtDatabaseVariableAttribute>();
                if (attribute == null) continue;

                string columnName = attribute.ColumnName;

                if (member is System.Reflection.PropertyInfo prop && prop.CanWrite)
                {
                    SetValueForMember(prop.PropertyType, value => prop.SetValue(instance, value), row, columnName);
                }
                else if (member is System.Reflection.FieldInfo field)
                {
                    SetValueForMember(field.FieldType, value => field.SetValue(instance, value), row, columnName);
                }
            }
        }
        private static void SetValueForMember(System.Type memberType, System.Action<object> assign, RowData row, string column)
        {
            object result = null;

            // Hücre verisini RowData içindeki tip güvenli metotlardan çekiyoruz
            if (memberType == typeof(int))
                result = row.GetIntValue(column, 0);
            else if (memberType == typeof(float))
                result = row.GetFloatValue(column, 0f);
            else if (memberType == typeof(string))
                result = row.GetStringValue(column, "");
            else if (memberType == typeof(bool))
            {
                CellData cellData = row.GetCellData(column);
                if (cellData != null && cellData.Value != null)
                    result = cellData.Value.Get<bool>();
            }
            else if (memberType.IsEnum)
            {
                string enumString = row.GetStringValue(column, "");
                if (System.Enum.TryParse(memberType, enumString, out var enumValue))
                    result = enumValue;
            }
            else
            {
                CellData cellData = row.GetCellData(column);
                if (cellData != null && cellData.Value != null)
                    result = cellData.Value.GetValue(); // ya da cellData.Value.Get<object>()
            }

            if (result != null)
                assign(result);
        }
        #endregion

    }
}