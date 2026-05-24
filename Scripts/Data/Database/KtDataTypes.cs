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
            return BitConverter.GetBytes(Value);
        }

        public override void Deserialize(byte[] data)
        {
            Value = BitConverter.ToBoolean(data, 0);
        }
    }

    // ── Array types — database stores comma-separated strings ─────────────────

    [Serializable]
    public class KtFloatArray : KtDataType
    {
        public float[] Values = Array.Empty<float>();
        public override object GetValue() => Values;
        public override T Get<T>() => (T)(object)Values;
        public override KtDataType Clone() => new KtFloatArray { Values = (float[])Values.Clone() };
        public override Type GetDataType() => typeof(float[]);

        public override bool ParseString(string stringData)
        {
            if (string.IsNullOrWhiteSpace(stringData)) { Values = Array.Empty<float>(); return true; }
            var parts = stringData.Split(',');
            Values = new float[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                if (!float.TryParse(parts[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out Values[i]))
                    return false;
            return true;
        }

        public override byte[] Serialize()
        {
            var bytes = new byte[4 + Values.Length * 4];
            Buffer.BlockCopy(BitConverter.GetBytes(Values.Length), 0, bytes, 0, 4);
            for (int i = 0; i < Values.Length; i++)
                Buffer.BlockCopy(BitConverter.GetBytes(Values[i]), 0, bytes, 4 + i * 4, 4);
            return bytes;
        }

        public override void Deserialize(byte[] data)
        {
            int count = BitConverter.ToInt32(data, 0);
            Values = new float[count];
            for (int i = 0; i < count; i++)
                Values[i] = BitConverter.ToSingle(data, 4 + i * 4);
        }
    }

    [Serializable]
    public class KtIntArray : KtDataType
    {
        public int[] Values = Array.Empty<int>();
        public override object GetValue() => Values;
        public override T Get<T>() => (T)(object)Values;
        public override KtDataType Clone() => new KtIntArray { Values = (int[])Values.Clone() };
        public override Type GetDataType() => typeof(int[]);

        public override bool ParseString(string stringData)
        {
            if (string.IsNullOrWhiteSpace(stringData)) { Values = Array.Empty<int>(); return true; }
            var parts = stringData.Split(',');
            Values = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                if (!int.TryParse(parts[i].Trim(), out Values[i]))
                    return false;
            return true;
        }

        public override byte[] Serialize()
        {
            var bytes = new byte[4 + Values.Length * 4];
            Buffer.BlockCopy(BitConverter.GetBytes(Values.Length), 0, bytes, 0, 4);
            for (int i = 0; i < Values.Length; i++)
                Buffer.BlockCopy(BitConverter.GetBytes(Values[i]), 0, bytes, 4 + i * 4, 4);
            return bytes;
        }

        public override void Deserialize(byte[] data)
        {
            int count = BitConverter.ToInt32(data, 0);
            Values = new int[count];
            for (int i = 0; i < count; i++)
                Values[i] = BitConverter.ToInt32(data, 4 + i * 4);
        }
    }

    [Serializable]
    public class KtStringArray : KtDataType
    {
        public string[] Values = Array.Empty<string>();
        public override object GetValue() => Values;
        public override T Get<T>() => (T)(object)Values;
        public override KtDataType Clone() => new KtStringArray { Values = (string[])Values.Clone() };
        public override Type GetDataType() => typeof(string[]);

        public override bool ParseString(string stringData)
        {
            if (string.IsNullOrWhiteSpace(stringData)) { Values = Array.Empty<string>(); return true; }
            Values = stringData.Split(',');
            for (int i = 0; i < Values.Length; i++)
                Values[i] = Values[i].Trim();
            return true;
        }

        public override byte[] Serialize()
        {
            var encoded = new System.Collections.Generic.List<byte>();
            encoded.AddRange(BitConverter.GetBytes(Values.Length));
            foreach (var s in Values)
            {
                var strBytes = System.Text.Encoding.UTF8.GetBytes(s ?? "");
                encoded.AddRange(BitConverter.GetBytes(strBytes.Length));
                encoded.AddRange(strBytes);
            }
            return encoded.ToArray();
        }

        public override void Deserialize(byte[] data)
        {
            int count = BitConverter.ToInt32(data, 0);
            Values = new string[count];
            int offset = 4;
            for (int i = 0; i < count; i++)
            {
                int len = BitConverter.ToInt32(data, offset); offset += 4;
                Values[i] = System.Text.Encoding.UTF8.GetString(data, offset, len); offset += len;
            }
        }
    }
}