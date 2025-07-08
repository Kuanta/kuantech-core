using System;

namespace Kuantech.Core.Database.Attributes
{
    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class KtDatabaseVariableAttribute : Attribute
    {
        public string ColumnName { get; }

        public KtDatabaseVariableAttribute(string columnName)
        {
            ColumnName = columnName;
        }
    }
}