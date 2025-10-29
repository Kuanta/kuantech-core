using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kuantech.Data;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Kuantech.Core.Database
{
    /// <summary>
    /// Sheet reader for database
    /// </summary>
    public class DatabaseSheetReader : MonoBehaviour
    {
        [SerializeField] private SheetReader SheetReader;
        public int MaxRow = 50;
        public async UniTask  UpdateDatabase(KtDatabase database)
        {
            List<DataTable> tables = database.GetTables();
            var tasks = new List<UniTask>();
            foreach (var table in tables)
            {
                string tableName = table.TableName;
                int rowCount = MaxRow;
                int colCount = table.Schema.Count;
                string sheetRange = GetSheetRange(tableName, rowCount+1, colCount+1); //+1 to include header row, +1 to include column id
                //Read sheet
                var task = ReadAndApply(table, sheetRange);
                tasks.Add(task);
            }
            await tasks;
        }

        private async UniTask ReadAndApply(DataTable table, string sheetRange)
        {
            table.BuildTable();
            
            JObject sheetData = await SheetReader.GetSheetDataAsync(sheetRange);
            if (sheetData != null)
            {
                var valuesArray = sheetData["values"] as JArray;
                if(valuesArray == null || valuesArray.Count <= 1)
                {
                    return;
                }

                var header = valuesArray[0] as JArray;
                if (header == null) return;
                
                Dictionary<int, string> _columnIndexByName = new Dictionary<int, string>();
                for (int i = 0; i < header.Count; i++)
                {
                    var columnName = header[i]?.ToString();
                    if (string.IsNullOrEmpty(columnName)) continue;

                    // Store the index of each column by its name
                    _columnIndexByName[i] = columnName;
                }
                
                for (int i = 1; i < valuesArray.Count; i++)
                {
                    var rowArray = valuesArray[i] as JArray;
                    if (rowArray == null || rowArray.Count == 0) continue;
          
                    //Get row id
                    var rowId = rowArray[0]?.ToString();
                    if (string.IsNullOrEmpty(rowId)) continue;

                    //Get row
                    DataTable.RowData row = table.GetRow(rowId);
                    
                    //If row is null, insert new row
                    if (row == null)
                    {
                        //Insert row
                        row = table.AddNewRow(rowId);
                    }
                    
                    if(row == null) continue; //Yet another safety check
                    
                    //Loop columns
                    for (int col = 1; col < header.Count && col < rowArray.Count; col++)
                    {
                        string columnName = _columnIndexByName[col];
                        KtDataType dataType = table.GetDataEntry(rowId, columnName);
                        if(dataType == null) continue;
          
                        string rawValue = rowArray[col]?.ToString() ?? "";
                        dataType.ParseString(rawValue);
                    }
        
                }
            }
        }
      
        #region Sheet Range Getter

        public string GetSheetRange(string tableName, int rowCount, int colCount)
        {
            string startCell = "A1";
            string endColumn = GetColumnLetter(colCount);
            string endCell = $"{endColumn}{rowCount}";
            return $"{tableName}!{startCell}:{endCell}";
        }
        
        // 1 -> A, 2 -> B, ..., 26 -> Z, 27 -> AA, 28 -> AB, ...
        private string GetColumnLetter(int col)
        {
            string column = "";
            while (col > 0)
            {
                col--; // Google Sheets is 1-indexed, so we decrement first
                column = (char)('A' + (col % 26)) + column;
                col /= 26;
            }
            return column;
        }
        #endregion

    }
}