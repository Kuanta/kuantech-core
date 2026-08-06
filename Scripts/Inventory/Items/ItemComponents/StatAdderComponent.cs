using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Rpg;
using Kuantech.Utils;

namespace Kuantech.Inventory
{
    public class StatAdderItemComponentData : ItemComponentData
    {
        public List<StatModifierData> ModifierDatas;
        public override ItemComponent CreateInstance()
        {
            return new StatAdderItemComponent(ModifierDatas);
        }
    }

    /// <summary>
    /// Gives an item an INTRINSIC stat contribution — a weapon's flat Damage, armor's Health, etc. — as
    /// StatModifiers on the wearer. These are the item's base stats (a grey item with no affixes still has
    /// them); random rolled bonuses are separate (AttributeAffix on top).
    ///
    /// It applies while the item is equipped AND attached to an actor. In the meta flow you equip in the
    /// menu (no actor yet) and it applies when the persistent inventory is attached to the run player at
    /// spawn (OnAttachedToActor). Values scale with the item's level through each modifier's Level.
    /// </summary>
    public class StatAdderItemComponent : ItemComponent
    {
        private readonly List<StatModifierData> _modifierDatas;
        private readonly List<StatModifier> _applied = new();

        /// <summary>
        /// The actor <see cref="_applied"/> was handed to. Tracked because an inventory is persistent and
        /// outlives the actors it visits, so "already applied" on its own says nothing about whether it is
        /// applied to the actor being asked about now.
        /// </summary>
        private Actor _appliedTo;

        public StatAdderItemComponent(List<StatModifierData> modifierDatas)
        {
            _modifierDatas = modifierDatas;
        }

        // The inventory just landed on a (new) actor — apply if this item is currently equipped. This is
        // the hook that makes a previously-equipped item take effect on each run's fresh player.
        public override void OnAttachedToActor(Actor actor)
        {
            if (ParentItem != null && ParentItem.IsEquipped()) Apply(actor);
        }

        public override void OnDetachedFromActor(Actor actor)
        {
            Remove(actor);
        }

        // Equipped while already attached to a live actor (in-run equipping). In the menu GetOwner() is
        // null, so this no-ops and OnAttachedToActor applies it when the inventory reaches the run player.
        public override void OnItemEquipped(Item item, EquipmentSlotType slotType)
        {
            Apply(GetOwner());
        }

        public override void OnItemUnequipped(Item item)
        {
            Remove(GetOwner());
        }

        private void Apply(Actor actor)
        {
            if (actor == null || _modifierDatas.IsNullOrEmpty()) return;

            // Already on this exact actor — applying again would stack a second copy of every modifier.
            if (_applied.Count > 0 && ReferenceEquals(_appliedTo, actor)) return;

            // Applied, but to somebody else. This is the normal state after an actor goes away without a
            // clean detach: Actor.Despawn defers its Cleanup to a coroutine, so a scene change destroys the
            // menu preview character before that ever runs, and nothing clears the bookkeeping. Treating
            // "already applied" as a reason to skip would then leave every later actor without its item
            // stats, so drop the stale modifiers instead and apply to the actor actually being asked about.
            if (_applied.Count > 0) Remove(_appliedTo);

            StatsModule stats = actor.GetModule<StatsModule>();
            if (stats == null) return;

            int itemLevel = ParentItem != null ? ParentItem.GetLevel() : 0;
            float itemScale = ParentItem != null ? ParentItem.GetScale() : 1f;
            foreach (var data in _modifierDatas)
            {
                if (data.Stat == null) continue;
                // Level = item level (upgrades) and Scale = item scale (rarity), so the modifier tracks both.
                StatModifier modifier = new StatModifier(data) { Level = itemLevel, Scale = itemScale };
                stats.AddModifier(modifier);
                _applied.Add(modifier);
            }
            _appliedTo = actor;
        }

        private void Remove(Actor actor)
        {
            if (_applied.Count == 0) return;

            // The actor may already be destroyed — this runs precisely when one went away. The null check
            // covers Unity's destroyed-object null, and the bookkeeping is cleared either way, which is the
            // part that matters: modifiers on a dead actor need no removal, but the record of them does.
            StatsModule stats = actor != null ? actor.GetModule<StatsModule>() : null;
            if (stats != null)
                foreach (var modifier in _applied)
                    stats.RemoveModifier(modifier);
            _applied.Clear();
            _appliedTo = null;
        }

        /// <summary>
        /// This item's total contribution to a single attribute at its current level (summed if several
        /// modifiers target the same attribute). Returns false when the item does not touch that attribute
        /// — a per-attribute UI indicator can then hide itself.
        /// </summary>
        public bool TryGetAttributeValue(AttributeAsset attribute, out float value)
        {
            value = 0f;
            if (attribute == null || _modifierDatas.IsNullOrEmpty()) return false;

            int itemLevel = ParentItem != null ? ParentItem.GetLevel() : 0;
            float itemScale = ParentItem != null ? ParentItem.GetScale() : 1f;
            bool found = false;
            foreach (var data in _modifierDatas)
            {
                if (data.Stat != attribute) continue;
                value += data.GetValue(itemLevel, itemScale);
                found = true;
            }
            return found;
        }

        /// <summary>
        /// This item's intrinsic stats as attribute/value pairs at its current level — the structured
        /// counterpart of <see cref="DescribeStats"/>, for a row-per-stat UI (mirrors WeaponComponent's
        /// damage breakdown). One entry per modifier; several targeting the same attribute stay separate.
        /// </summary>
        public List<AttributeValue> GetStatBreakdown()
        {
            var lines = new List<AttributeValue>();
            if (_modifierDatas.IsNullOrEmpty()) return lines;

            int itemLevel = ParentItem != null ? ParentItem.GetLevel() : 0;
            float itemScale = ParentItem != null ? ParentItem.GetScale() : 1f;
            foreach (var data in _modifierDatas)
            {
                if (data.Stat == null) continue;
                lines.Add(new AttributeValue(data.Stat, data.GetValue(itemLevel, itemScale)));
            }
            return lines;
        }

        /// <summary>
        /// Human-readable stat lines for the item details panel, e.g. "+5 Damage", at the item's current
        /// level. Mirrors AttributeAffix.Stringfy so base stats and affixes read the same in the UI.
        /// </summary>
        public List<string> DescribeStats()
        {
            var lines = new List<string>();
            if (_modifierDatas.IsNullOrEmpty()) return lines;

            int itemLevel = ParentItem != null ? ParentItem.GetLevel() : 0;
            float itemScale = ParentItem != null ? ParentItem.GetScale() : 1f;
            foreach (var data in _modifierDatas)
            {
                if (data.Stat == null) continue;
                float value = data.GetValue(itemLevel, itemScale);
                lines.Add($"+{value} {data.Stat.GetName()}");
            }
            return lines;
        }

        public override void OnItemAdded(Item item) { }
        public override void OnItemRemoved(Item item) { }
        public override void OnItemUsed(Item item) { }
    }
}
