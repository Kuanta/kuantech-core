using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core
{
    [Serializable]
    public struct ClassTypeEntry
    {
        public string Id;
        [Tooltip("Fully-qualified C# type name, e.g. \"Kuantech.HordeBonkers.BudgetSpentTrigger, Assembly-CSharp\".")]
        public string TypeName;
    }

    /// <summary>
    /// Id &lt;-&gt; Type registry, hand-authored in the Inspector (see ClassTypeEntry) rather than a plain
    /// Singleton -- Singleton&lt;T&gt; auto-creates an empty instance if none exists in the current scene, which
    /// would silently lose whatever list was authored elsewhere. As a SubManager this lives once on
    /// GameManager.prefab (DontDestroy) and is reachable from anywhere via GetContext, same as
    /// AssetCollection/CurrencyManager.
    ///
    /// Backs short type aliases in JSON "$type" values (see ClassTypeSerializationBinder) so a
    /// Reward/WaveEventTrigger/WaveEventAction subclass can be named e.g. "BudgetSpent" in Arenas.json
    /// instead of its full namespace-qualified C# name -- renaming or moving the class only means updating
    /// one row here, not every JSON file that references it.
    /// </summary>
    public class ClassTypeGetter : SubManager
    {
        public List<ClassTypeEntry> Entries;

        private Dictionary<string, Type> _typesById;
        private Dictionary<Type, string> _idsByType;

        // Built on first access, not on Initialize -- there's no manager-lifecycle dependency here, just a
        // hand-authored lookup table.
        private void EnsureBuilt()
        {
            if (_typesById != null) return;

            _typesById = new Dictionary<string, Type>();
            _idsByType = new Dictionary<Type, string>();
            if (Entries == null) return;

            foreach (var entry in Entries)
            {
                if (string.IsNullOrEmpty(entry.Id) || string.IsNullOrEmpty(entry.TypeName)) continue;

                Type type = Type.GetType(entry.TypeName);
                if (type == null)
                {
                    Debug.LogError($"[ClassTypeGetter] Unknown type '{entry.TypeName}' for id '{entry.Id}'.");
                    continue;
                }

                _typesById[entry.Id] = type;
                _idsByType[type] = entry.Id;
            }
        }

        public static Type GetType(string id)
        {
            var ctx = GetContext<ClassTypeGetter>();
            if (ctx == null || string.IsNullOrEmpty(id)) return null;
            ctx.EnsureBuilt();
            return ctx._typesById.TryGetValue(id, out var type) ? type : null;
        }

        public static string GetId(Type type)
        {
            var ctx = GetContext<ClassTypeGetter>();
            if (ctx == null || type == null) return null;
            ctx.EnsureBuilt();
            return ctx._idsByType.TryGetValue(type, out var id) ? id : null;
        }
    }
}
