using System;

namespace Kuantech.Core.Utils
{
    public class ResettableAttribute : Attribute
    {
        public object DefaultVal { get; private set; }

        public ResettableAttribute(object defaultVal = null)
        {
            DefaultVal = defaultVal;
        }
    }
}