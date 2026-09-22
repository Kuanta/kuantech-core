using System;

namespace Kuantech.Core.Combat
{
    /// <summary>
    /// Abstract on purpose: SubclassSelectorDrawer lists every non-abstract type assignable to the field,
    /// so a concrete base would show up in the dropdown as a filter that filters nothing.
    /// </summary>
    [Serializable]
    public abstract class TargetFilter
    {
        public abstract bool FilterOutTarget(Actor self, Actor target);
    }
}
