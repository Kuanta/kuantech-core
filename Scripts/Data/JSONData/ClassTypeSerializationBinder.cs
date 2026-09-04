using System;
using Kuantech.Core;
using Newtonsoft.Json.Serialization;

namespace Kuantech.Core.Data
{
    /// <summary>
    /// Lets a JSON "$type" value be a short id ("BudgetSpent", resolved through ClassTypeGetter) instead of
    /// a full "Namespace.Class, Assembly" C# name. Falls back to treating the value as a fully-qualified
    /// name when it isn't a registered alias, so existing $type strings (or a type nobody bothered
    /// aliasing) keep working unchanged.
    /// </summary>
    public class ClassTypeSerializationBinder : ISerializationBinder
    {
        public Type BindToType(string assemblyName, string typeName)
        {
            Type aliased = ClassTypeGetter.GetType(typeName);
            if (aliased != null) return aliased;

            string fullName = string.IsNullOrEmpty(assemblyName) ? typeName : $"{typeName}, {assemblyName}";
            return Type.GetType(fullName);
        }

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            string alias = ClassTypeGetter.GetId(serializedType);
            if (!string.IsNullOrEmpty(alias))
            {
                assemblyName = null;
                typeName = alias;
                return;
            }

            assemblyName = serializedType.Assembly.GetName().Name;
            typeName = serializedType.FullName;
        }
    }
}
