using System;

namespace Kuantech.Data
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SheetFloatArrayDataColumnAttribute : Attribute
    {
        public int ColumnIndex { get; }

        public SheetFloatArrayDataColumnAttribute(int columnIndex)
        {
            ColumnIndex = columnIndex;
        }
    }
}