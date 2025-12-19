using System;
using System.Globalization;

namespace Kuantech.Core.Database
{
    [Serializable]
    public abstract class KtDataType : ISaveable
    {
        public abstract object GetValue();
        public abstract T Get<T>();
        public abstract KtDataType Clone();

        public abstract Type GetDataType();
        
        //Tries to parse a string representation of the data type.
        public abstract bool ParseString(string stringData);
        public abstract byte[] Serialize();

        public abstract void Deserialize(byte[] data);
    }

    [Serializable]
    public class KtFloat : KtDataType
    {
        public float Value;
        public override object GetValue() => Value;
        public override T Get<T>() => (T)(object)Value;
        public override KtDataType Clone() => new KtFloat { Value = this.Value };
        public override Type GetDataType()
        {
            return typeof(float);
        }

        public override bool ParseString(string stringData)
        {
            if (float.TryParse(stringData,NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
            {
                Value = floatValue;
                return true;
            }

            return false;
        }

        public override byte[] Serialize()
        {
            //Serialize float value
            return BitConverter.GetBytes(Value);
        }

        public override void Deserialize(byte[] data)
        {
            //Deserialize float value
            Value = BitConverter.ToSingle(data, 0);
        }
    }

    [Serializable]
    public class KtInt : KtDataType
    {
        public int Value;
        public override object GetValue() => Value;
        public override T Get<T>() => (T)(object)Value;
        public override KtDataType Clone() => new KtInt{ Value = this.Value };
        public override Type GetDataType()
        {
            return typeof(int);
        }
        public override bool ParseString(string stringData)
        {
            if (Int32.TryParse(stringData,out int intValue))
            {
                Value = intValue;
                return true;
            }
            return false;
        }

        public override byte[] Serialize()
        {
            //Serialize int value
            return BitConverter.GetBytes(Value);
        }

        public override void Deserialize(byte[] data)
        {
            //Deserialize int value
            Value = BitConverter.ToInt32(data, 0);
        }
    }

    [Serializable]
    public class KtString : KtDataType
    {
        public string Value;
        public override object GetValue() => Value;
        public override T Get<T>() => (T)(object)Value;
        public override KtDataType Clone() => new KtString{ Value = this.Value };
        public override Type GetDataType()
        {
            return typeof(string);
        }
        public override bool ParseString(string stringData)
        {
            Value = stringData;
            return true;
        }

        public override byte[] Serialize()
        {
            //Serialize string to byte array using UTF8 encoding
            return System.Text.Encoding.UTF8.GetBytes(Value);
        }

        public override void Deserialize(byte[] data)
        {
            //Deserialize byte array to string using UTF8 encoding
            Value = System.Text.Encoding.UTF8.GetString(data);
        }
    }

    [Serializable]
    public class KtBool : KtDataType
    {
        public bool Value;
        public override object GetValue() => Value;
        public override T Get<T>() => (T)(object)Value;
        public override KtDataType Clone() => new KtBool{ Value = this.Value };
        public override Type GetDataType()
        {
            return typeof(bool);
        }
        public override bool ParseString(string stringData)
        {
            if (Int32.TryParse(stringData,out int intValue))
            {
                Value = intValue > 0;
                return true;
            }
            return false;
        }

        public override byte[] Serialize()
        {
            //Serialize bool value
            return BitConverter.GetBytes(Value);
        }

        public override void Deserialize(byte[] data)
        {
            //Deserialize bool value
            Value = BitConverter.ToBoolean(data, 0);
        }
    }
}