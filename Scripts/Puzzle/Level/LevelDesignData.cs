using System;
using Kuantech.Data;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Kuantech.Puzzle
{
    [Serializable]
    public class LevelDesignData
    {
        
        public virtual bool CreateFromDesignAsset(LevelDesignAsset levelDesignAsset)
        {
            throw new NotImplementedException();
        }

        public virtual bool CreateFromSheetData(JObject sheetData, int levelIndex)
        {
            JArray values = (JArray) sheetData["values"];
            if (values == null)
            {
                Debug.LogError("Couldn't read data sheet");
                return false;
            }
            if (values.Count - 1 <= levelIndex)
            {
                levelIndex = values.Count - 2; //Last row
            }
            
            JToken row = values[levelIndex + 1];
            ParseSheetData(row);
            return true;
        }

        public virtual void ParseSheetData(JToken row)
        {
            ParseNumericFields(row);
        }
        protected void ParseNumericFields(JToken row)
        {
            var fields = GetType().GetFields();

            foreach (var field in fields)
            {
                // INT Data Attribute
                var intAttribute = (SheetIntDataColumnAttribute) Attribute.GetCustomAttribute(field, typeof(SheetIntDataColumnAttribute));
                if (intAttribute != null)
                {
                    int value = SheetReader.ReadIntData(row, intAttribute.ColumnIndex, intAttribute.DefaultValue);
                    field.SetValue(this, value);
                    continue;
                }

                // FLOAT Data Attribute
                var floatAttribute = (SheetFloatDataColumnAttribute) Attribute.GetCustomAttribute(field, typeof(SheetFloatDataColumnAttribute));
                if (floatAttribute != null)
                {
                    float value = SheetReader.ReadFloatData(row, floatAttribute.ColumnIndex, floatAttribute.DefaultValue);
                    field.SetValue(this, value);
                    continue;
                }

                var boolAttribute =
                    (SheetBoolDataColumnAttribute) Attribute.GetCustomAttribute(field,
                        typeof(SheetBoolDataColumnAttribute));
                if (boolAttribute != null)
                {
                    bool value = SheetReader.ReadBoolData(row, boolAttribute.ColumnIndex, boolAttribute.DefaultValue);
                    field.SetValue(this, value);
                    continue;
                }
                
                //Float Array Data Attribute
                var floatArrayAttribute =
                    (SheetFloatArrayDataColumnAttribute) Attribute.GetCustomAttribute(field,
                        typeof(SheetFloatArrayDataColumnAttribute));
                if (floatArrayAttribute != null)
                {
                    float[] values = SheetReader.ReadFloatArrayData(row, floatArrayAttribute.ColumnIndex);
                    field.SetValue(this, values);
                    continue;
                }
                
                var stringAttribute =
                    (SheetStringDataColumnAttribute) Attribute.GetCustomAttribute(field,
                        typeof(SheetStringDataColumnAttribute));
                if (stringAttribute != null)
                {
                    string value = row[stringAttribute.ColumnIndex].ToString();
                    field.SetValue(this, value);
                    continue;
                }
            }
        }
    }
}