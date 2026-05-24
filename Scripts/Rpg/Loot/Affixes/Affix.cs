
using Kuantech.Core.Database.Attributes;
using Kuantech.Core.Database;
using Kuantech.Core;
using System;
using Kuantech.Inventory;
using UnityEngine;

namespace Kuantech.Rpg
{
    /// <summary>
    /// Bonuses to items
    /// </summary>
    public class Affix
    {
        [KtDatabaseVariable("ID")] public string AffixId { get; protected set; }
        [KtDatabaseVariable("AffixName")] public string AffixName { get; protected set; }
        [KtDatabaseVariable("Weight")] public float Weight { get; protected set; }

        [NonSerialized] public Item AttachedItem;

        [Serializable]
        protected class AffixBaseState
        {
            public string AffixId;
            public string AffixName;
            public float Weight;
        }

        public virtual void ReadFromRow(DataTable.RowData row)
        {
            DataTable.SetVariablesFromRow(this, row);
        }

        public void AttachToItem(Item item)
        {
            AttachedItem = item;
        }

        public void DetachFromItem()
        {
            AttachedItem = null;
        }

        public virtual Affix Clone()
        {
            var clone = (Affix)MemberwiseClone();
            clone.AttachedItem = null;
            return clone;
        }

        public virtual string SerializeAffix()
        {
            return JsonUtility.ToJson(new AffixBaseState { AffixId = AffixId, AffixName = AffixName, Weight = Weight });
        }

        public virtual void DeserializeAffix(string data)
        {
            var state = JsonUtility.FromJson<AffixBaseState>(data);
            AffixId = state.AffixId;
            AffixName = state.AffixName;
            Weight = state.Weight;
        }

        public virtual void ApplyAffixToActor(Actor actor) { }

        public virtual void RemoveAffixFromActor(Actor actor) { }

        public virtual string Stringfy() => "";
    }
}
