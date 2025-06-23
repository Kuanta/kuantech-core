using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Kuantech.Core.Database
{
    public class ScriptableObjectDatabase : KtDatabase
    {
        public List<DataTable> Tables;
        private Dictionary<string, DataTable> _tablesLookup;
        public override async UniTask Initialize()
        {
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

        public override KtDataType GetEntry(string table, string entryId, string key)
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

    }
}