using Kuantech.Core;
using System;
using Kuantech.Inventory;

namespace Kuantech.Rpg
{
    /// <summary>
    /// Authorable definition of an affix — the "template" side, editable in the inspector through a
    /// [SerializeReference] list. Produces a runtime <see cref="Affix"/> via <see cref="CreateAffix"/>,
    /// mirroring ItemComponentData -> ItemComponent. Definitions are immutable content; the rolled/leveled
    /// state lives on the runtime Affix.
    /// </summary>
    [Serializable]
    public abstract class AffixData
    {
        public string AffixId;
        public string AffixName;
        public float Weight = 1f;

        /// <summary>
        /// Builds the runtime affix from this definition. Subclasses only construct their typed affix and set
        /// their own fields in <see cref="Instantiate"/>; the shared identity (id, name, weight) is copied
        /// here so no subclass can forget it.
        /// </summary>
        public Affix CreateAffix()
        {
            Affix affix = Instantiate();
            affix.AffixId = AffixId;
            affix.AffixName = AffixName;
            affix.Weight = Weight;
            return affix;
        }

        protected abstract Affix Instantiate();
    }

    /// <summary>
    /// Runtime instance of an affix on an item — created from an <see cref="AffixData"/> definition, then
    /// attached to an item and applied to its wearer. Holds the affix's live state (level, attached item);
    /// the authorable definition lives in AffixData.
    /// </summary>
    public abstract class Affix
    {
        public string AffixId;
        public string AffixName;
        public float Weight;

        [NonSerialized] public Item AttachedItem;

        public void AttachToItem(Item item)
        {
            AttachedItem = item;
        }

        public void DetachFromItem()
        {
            AttachedItem = null;
        }

        /// <summary>
        /// Sets the affix's level (used to scale its magnitude, e.g. to the owning item's level at
        /// generation). Base affixes have no level; typed affixes override this.
        /// </summary>
        public virtual void SetAffixLevel(int level) { }

        public virtual void ApplyAffixToActor(Actor actor) { }

        public virtual void RemoveAffixFromActor(Actor actor) { }

        public virtual string Stringfy() => "";

        /// <summary>
        /// Serializes ONLY this affix's mutable runtime state (e.g. level, rolled values). Identity and
        /// definition-derived fields are rebuilt from the AffixData registry on load, so they are not stored
        /// here. Default: nothing to save (affixes with no mutable state need not override).
        /// </summary>
        public virtual string SerializeAffix() => null;

        public virtual void DeserializeAffix(string data) { }
    }
}
