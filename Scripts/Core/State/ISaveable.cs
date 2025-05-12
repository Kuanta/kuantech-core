using System;
using System.Collections.Generic;
using System.IO;

namespace Kuantech.Core
{
    [Serializable]
    public struct SaveableData
    {
        public Dictionary<string, byte[]> FieldData; // Field name → binary
        public byte[] ManualData;

        public byte[] ToBytes()
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            writer.Write(FieldData.Count);
            foreach (var pair in FieldData)
            {
                writer.Write(pair.Key);
                writer.Write(pair.Value.Length);
                writer.Write(pair.Value);
            }

            writer.Write(ManualData?.Length ?? 0);
            if (ManualData != null)
                writer.Write(ManualData);

            return ms.ToArray();
        }

        public static SaveableData FromBytes(byte[] data)
        {
            var result = new SaveableData { FieldData = new Dictionary<string, byte[]>() };

            using var ms = new MemoryStream(data);
            using var reader = new BinaryReader(ms);

            int fieldCount = reader.ReadInt32();
            for (int i = 0; i < fieldCount; i++)
            {
                string name = reader.ReadString();
                int len = reader.ReadInt32();
                result.FieldData[name] = reader.ReadBytes(len);
            }

            int manualLen = reader.ReadInt32();
            result.ManualData = reader.ReadBytes(manualLen);

            return result;
        }
    }

    
    public interface ISaveable
    {
        public byte[] Serialize();
        public void Deserialize(byte[] data);
    }
}