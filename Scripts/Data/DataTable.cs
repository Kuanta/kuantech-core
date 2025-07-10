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

        
    }
}