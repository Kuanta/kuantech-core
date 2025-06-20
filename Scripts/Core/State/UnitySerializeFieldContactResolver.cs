namespace Kuantech.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using UnityEngine;

    public class UnitySerializeFieldContractResolver : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            // Public ve private field’ları al
            var fields = type.GetFields(flags)
                .Where(f =>
                    f.IsPublic || f.GetCustomAttribute<SerializeField>() != null
                );

            var properties = new List<JsonProperty>();

            foreach (var field in fields)
            {
                var prop = base.CreateProperty(field, memberSerialization);
                prop.Writable = true;
                prop.Readable = true;
                properties.Add(prop);
            }

            return properties;
        }
    }

}