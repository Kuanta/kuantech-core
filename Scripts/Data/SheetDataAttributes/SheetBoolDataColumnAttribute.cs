using System;

namespace Kuantech.Data
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SheetBoolDataColumnAttribute : Attribute
    {
        public int ColumnIndex { get; }
        public bool DefaultValue { get; }

        public SheetBoolDataColumnAttribute(int columnIdnex, bool defaultValue = true)
        {
            ColumnIndex = columnIdnex;
            DefaultValue = defaultValue;
        }
    }
}