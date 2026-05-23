using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Inventory;
using Kuantech.Rpg.Skills;

namespace Kuantech.Rpg
{
    public class BuffAdderItemComponentData : ItemComponentData
    {
        public List<SkillDataAsset> SkillToAdd;
        public List<PassiveSkillDataAsset> PassiveSkillsToAdd;
        public List<StatModifierData> Modifiers;
        public override ItemComponent CreateInstance() => new BuffAdderItemComponent(this);
    }

    public class BuffAdderItemComponent : ItemComponent
    {
        public BuffAdderItemComponentData Data;
        private readonly List<StatModifier> _appliedModifiers = new();
        public bool AddBuffsOnEquip;
        public BuffAdderItemComponent(BuffAdderItemComponentData data) => Data = data;

        public override void OnItemAdded(Item item) { }
        public override void OnItemEquipped(Item item, EquipmentSlotType slotType)
        {
            if (AddBuffsOnEquip) 
            {
                AddBuffToActor(item.GetOwner());
            }
        }
        public override void OnItemRemoved(Item item) { }
        public override void OnItemUnequipped(Item item)
        {
            if (AddBuffsOnEquip)
            {
                RemoveBuffFromActor(item.GetOwner());
            }
        }
        public override void OnItemUsed(Item item) { }

        public void AddBuffToActor(Actor actor)
        {
            if(actor == null)
            {
                return;
            }
            StatsModule stats = actor.GetModule<StatsModule>();
            if (stats != null && Data.Modifiers != null)
            {
                foreach (var modData in Data.Modifiers)
                {
                    var mod = new StatModifier(modData)
                    {
                        Level = ParentItem.GetLevel()
                    };
                    _appliedModifiers.Add(mod);
                    stats.AddModifier(mod);
                }
            }
             
            //todo: Rank skills
            SpellBook spellBook = actor.GetModule<SpellBook>();
            if (spellBook != null)
            {
                if (Data.SkillToAdd != null)
                {
                    foreach (var skillAsset in Data.SkillToAdd)
                    {
                        spellBook.RegisterSkill(skillAsset);
                    }

                    if (Data.PassiveSkillsToAdd != null)
                    {
                        foreach (var passiveAsset in Data.PassiveSkillsToAdd)
                        {
                            spellBook.RegisterPassiveSkill(passiveAsset);
                        }
                    }
                }

            }
        }

        public void RemoveBuffFromActor(Actor actor)
        {
            if (actor == null)
            {
                return;
            }
            StatsModule stats = actor.GetModule<StatsModule>();
            if (stats != null && _appliedModifiers.Count > 0)
            {
                stats.RemoveModifiers(_appliedModifiers);
                _appliedModifiers.Clear();
            }

            SpellBook spellBook = actor.GetModule<SpellBook>();
            if (spellBook != null)
            {
                if (Data.SkillToAdd != null)
                    foreach (var skillAsset in Data.SkillToAdd)
                        spellBook.UnregisterSkill(skillAsset);

                if (Data.PassiveSkillsToAdd != null)
                    foreach (var passiveAsset in Data.PassiveSkillsToAdd)
                        spellBook.UnregisterPassiveSkill(passiveAsset);
            }
        }
    }
}
