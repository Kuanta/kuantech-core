using System;
using Kuantech.Rpg;
using UnityEngine;

namespace Kuantech.Core
{
    /// <summary>
    /// A float value that scales with an actor's attribute.
    /// Result = BaseValue + Attribute * AttributeScale.
    /// Use GetValue(stats) wherever a plain float would otherwise be hardcoded.
    /// </summary>
    [Serializable]
    public class ScaledValue
    {
        public float BaseValue;
        public AttributeAsset Attribute;
        [Tooltip("Multiplied against the attribute's value before adding to BaseValue")]
        public float AttributeScale = 1f;

        public float GetValue(StatsModule stats = null)
        {
            if (stats == null || Attribute == null) return BaseValue;
            return BaseValue + stats.GetAttributeValue(Attribute) * AttributeScale;
        }

        /// <summary>Implicit cast so ScaledValue can replace plain floats in existing code.</summary>
        public static implicit operator float(ScaledValue v) => v?.BaseValue ?? 0f;
    }
}
