using System;
using Kuantech.Core;
using Kuantech.Rpg.Skills;

namespace Kuantech.Midcore
{
    /// <summary>
    /// Grants a passive skill via the trait — "1% chance to explode on hit" and the like. The trait rank
    /// becomes the passive's rank, so its proc chance / damage scale with how many ranks the player bought.
    /// Reuses the SpellBook passive system (TryProc + PassiveEffect); this applier is just the bridge.
    /// </summary>
    [Serializable]
    public class PassiveSkillTraitApplier : TraitApplier
    {
        public PassiveSkillDataAsset PassiveSkillAsset;

        public override void ApplyToActor(Actor actor, int rank)
        {
            if (actor == null || PassiveSkillAsset == null) return;
            SpellBook spellBook = actor.GetModule<SpellBook>();
            if (spellBook == null) return;

            // AddPassiveSkill returns null if the actor already has it; fall back to the existing one so a
            // trait sharing a passive with another source still gets its rank applied.
            PassiveSkill passive = spellBook.AddPassiveSkill(PassiveSkillAsset);
            if (passive == null) passive = spellBook.GetPassiveSkill(PassiveSkillAsset.SkillId);
            if (passive != null) passive.Rank = rank;
        }
    }
}
