using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kuantech.Core
{
    /// <summary>
    /// A json converter class to handle ISaveableLists
    /// </summary>
    public class SaveableJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
            => typeof(ISaveable).IsAssignableFrom(objectType);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var bytes = Kuantech.Core.SaveUtility.Serialize((ISaveable)value);
            // Base64 string olarak yazıyoruz (JSON içinde güvenli)
            writer.WriteValue(Convert.ToBase64String(bytes));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Base64 string bekliyoruz
            var token = JToken.Load(reader);
            if (token.Type == JTokenType.Null) return null;

            var base64 = token.ToObject<string>();
            var bytes = Convert.FromBase64String(base64);

            object instance;
            if (objectType.IsValueType)
            {
                // struct ise default instance yarat
                instance = Activator.CreateInstance(objectType);
            }
            else
            {
                instance = existingValue ?? Activator.CreateInstance(objectType);
            }

            Kuantech.Core.SaveUtility.Deserialize(bytes, (ISaveable)instance);
            return instance;
        }
    }
}
