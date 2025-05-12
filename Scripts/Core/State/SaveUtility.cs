using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;

namespace Kuantech.Core
{
    [AttributeUsage(AttributeTargets.Field)]
    public class SaveableFieldAttribute : System.Attribute
    {
        
    }

    public static class SaveUtility
    {
        public static byte[] Serialize(ISaveable target)
        {
            var data = CreateSaveableData(target);
            return data.ToBytes();
        }

        public static void Deserialize(byte[] bytes, ISaveable target)
        {
            var data = SaveableData.FromBytes(bytes);
            ApplySaveableData(data, target);
        }

        public static SaveableData CreateSaveableData(ISaveable target)
        {
            var fieldData = new Dictionary<string, byte[]>();
            var fields = target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                if (!System.Attribute.IsDefined(field, typeof(SaveableFieldAttribute))) continue;

                var value = field.GetValue(target);
                byte[] serialized = SerializeFieldValue(value);
                fieldData[field.Name] = serialized;
            }

            var manualData = target.Serialize();

            return new SaveableData
            {
                FieldData = fieldData,
                ManualData = manualData
            };
        }

        public static void ApplySaveableData(SaveableData data, ISaveable target)
        {
            var fields = target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                if (!System.Attribute.IsDefined(field, typeof(SaveableFieldAttribute))) continue;

                if (!data.FieldData.TryGetValue(field.Name, out var fieldBytes))
                {
                    Debug.LogWarning($"Field {field.Name} not found in SaveData for type {target.GetType().Name}");
                    continue;
                }

                DeserializeFieldValue(field, target, field.FieldType, fieldBytes);
            }

            target.Deserialize(data.ManualData);
        }

        private static byte[] SerializeFieldValue(object value)
        {
            switch (value)
            {
                case ISaveable nested:
                    return Serialize(nested); // Recursive
                default:
                    string json = JsonConvert.SerializeObject(value, Formatting.None, new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Auto
                    });
                    return System.Text.Encoding.UTF8.GetBytes(json);
            }
        }

        private static void DeserializeFieldValue(FieldInfo field, object owner, Type fieldType, byte[] bytes)
        {
            if (typeof(ISaveable).IsAssignableFrom(fieldType))
            {
                var nestedInstance = field.GetValue(owner) as ISaveable;

                if (nestedInstance == null)
                {
                    Debug.LogError($"[SaveUtility] ISaveable field '{field.Name}' is null on {owner.GetType().Name}");
                    return;
                }

                Deserialize(bytes, nestedInstance);
            }
            else
            {
                string json = System.Text.Encoding.UTF8.GetString(bytes);
                var obj = JsonConvert.DeserializeObject(json, fieldType, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto
                });

                field.SetValue(owner, obj);
            }
        }

    }
}
