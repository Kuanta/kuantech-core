using System;

namespace Kuantech.Data
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SheetIntDataColumnAttribute : Attribute
    {
        public int ColumnIndex { get; }
        public int DefaultValue { get; }

        public SheetIntDataColumnAttribute(int columnIndex, int defaultValue = 0)
        {
            ColumnIndex = columnIndex;
            DefaultValue = defaultValue;
        }
    }
}