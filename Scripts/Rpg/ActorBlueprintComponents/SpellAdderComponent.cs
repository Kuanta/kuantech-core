using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Rpg.Skills;
using Kuantech.Utils;

namespace Kuantech.ConveyorDefense
{
    public class SpellAdderComponent : ActorBlueprintComponent
    {
        public List<SkillDataAsset> SkillsToAdd;
        public override void OnActorCreated(ActorBlueprint blueprint, Actor actor)
        {
            if (SkillsToAdd.IsNullOrEmpty()) return;
            SpellBook spellBook = actor.GetModule<SpellBook>();
            if (spellBook == null) return;
            foreach (var skill in SkillsToAdd)
            {
                spellBook.AddSkill(skill);
            }
        }
    }
}