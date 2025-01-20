using System;

namespace Kuantech.Data
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SheetFloatDataColumnAttribute : Attribute
    {
        public int ColumnIndex { get; }
        public float DefaultValue { get; }

        public SheetFloatDataColumnAttribute(int columnIndex, float defaultValue = 0f)
        {
            ColumnIndex = columnIndex;
            DefaultValue = defaultValue;
        }
    }
}