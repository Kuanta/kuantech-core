using System;

namespace Kuantech.Core.Database
{
    [Serializable]
    public abstract class KtDataType
    {
        public abstract object GetValue();
        public abstract T Get<T>();
        public abstract KtDataType Clone();
    }

    [Serializable]
    public class KtFloat : KtDataType
    {
        public float Value;
        public override object GetValue() => Value;
        public override T Get<T>() => (T)(object)Value;
        public override KtDataType Clone() => new KtFloat { Value = this.Value };
    }

    [Serializable]
    public class KtInt : KtDataType
    {
        public int Value;
        public override object GetValue() => Value;
        public override T Get<T>() => (T)(object)Value;
        public override KtDataType Clone() => new KtInt{ Value = this.Value };
    }

    [Serializable]
    public class KtString : KtDataType
    {
        public string Value;
        public override object GetValue() => Value;
        public override T Get<T>() => (T)(object)Value;
        public override KtDataType Clone() => new KtString{ Value = this.Value };
    }

    [Serializable]
    public class KtBool : KtDataType
    {
        public bool Value;
        public override object GetValue() => Value;
        public override T Get<T>() => (T)(object)Value;
        public override KtDataType Clone() => new KtBool{ Value = this.Value };
    }
}