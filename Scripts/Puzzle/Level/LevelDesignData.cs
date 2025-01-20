using System;
using Kuantech.Data;
using Newtonsoft.Json.Linq;

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
            throw new NotImplementedException();
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
            }
        }
    }
}