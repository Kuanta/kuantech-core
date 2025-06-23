using System;

namespace Kuantech.Core.Database
{
    [Serializable]
    public class KtDataEntry
    {
        public KtDataType TypedValue { get; }

        public KtDataEntry(KtDataType data) => TypedValue = data;
        public T Get<T>() => TypedValue.Get<T>();

        public Type GetDataType()
        {
            return TypedValue.GetDataType();
        }
        
    }
}