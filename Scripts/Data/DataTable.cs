using System;
using System.Collections.Generic;
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
        public class RowData
        {
            public string Id;

            [SerializeReference]
            public List<KtDataType> Values;
        }
        
        [Button("Add New Row")]
        public void AddNewRow(string rowId)
        {
            var row = new RowData {Id = rowId};
            row.Values = new List<KtDataType>();
            foreach (var column in Schema)
            {
                var clone = CloneKtData(column.Data);
                row.Values.Add(clone);
            }

            Rows.Add(row);
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

        public RowData GetRow(string rowId)
        {
            if (_rowLookup.IsNullOrEmpty() || !_rowLookup.ContainsKey(rowId)) return null;
            return _rowLookup[rowId];
        }

        public KtDataType GetDataEntry(string rowId, string entryKey)
        {
            RowData rowData = GetRow(rowId);
            if (rowData == null) return null;
            
            int columnIndex = Schema.FindIndex(c => c.Name ==entryKey);
            if (columnIndex < 0)
            {
                return null;
            }
            return rowData.Values[columnIndex];
        }
    }
}