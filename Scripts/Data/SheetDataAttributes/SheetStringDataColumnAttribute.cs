using System;

namespace Kuantech.Data
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SheetStringDataColumnAttribute : Attribute
    {
        public int ColumnIndex { get; }
        public string DefaultValue { get; }

        public SheetStringDataColumnAttribute(int columnIndex, string defaultValue = "")
        {
            ColumnIndex = columnIndex;
            DefaultValue = defaultValue;
        }
    }
}