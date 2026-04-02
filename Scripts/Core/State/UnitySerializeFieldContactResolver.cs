namespace Kuantech.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    public class UnitySerializeFieldContractResolver : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var props = base.CreateProperties(type, memberSerialization);

            props = props
                .Where(p =>
                {
                    // Get the backing field or property
                    var field = type.GetField(p.UnderlyingName,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    if (field == null) return true;

                    // Skip if [NonSaveableField] is present
                    return !Attribute.IsDefined(field, typeof(NonSaveableFieldAttribute));
                })
                .ToList();

            return props;
        }
    }

}