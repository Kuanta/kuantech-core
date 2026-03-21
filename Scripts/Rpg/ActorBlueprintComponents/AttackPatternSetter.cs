using Kuantech.Core;
using Kuantech.Core.Database;
using Kuantech.Rpg.Managers;
using Kuantech.Rpg.Skills;
using Kuantech.Utils;

namespace Kuantech.ConveyorDefense
{
    public class AttackPatternSetter : ActorBlueprintComponent
    {
        public AttackPattern AttackPattern;
        
        public override void OnActorCreated(ActorBlueprint blueprint, Actor actor)
        {
            CombatModule cm = actor.GetModule<CombatModule>();
            if (cm == null) return;
            cm.SetCurrentAttackPattern(AttackPattern);
            SpellBook spellBook = actor.GetModule<SpellBook>();
            if (spellBook != null && AttackPattern.SkillToCast != null &&
                !spellBook.HasSkill(AttackPattern.SkillToCast))
            {
                spellBook.AddSkill(AttackPattern.SkillToCast);
            }
        }
        
        public void UpdateVariablesFromDatabase(KtDatabase database, string tableName, string rowId)
        {
            AttackPattern.AttackImplementationTime = database.GetFloat(tableName, rowId, "AttackTime", AttackPattern.AttackImplementationTime);
            AttackPattern.AnimationTime = database.GetFloat(tableName, rowId, "AnimationTime", AttackPattern.AnimationTime);
            AttackPattern.AttackDuration = database.GetFloat(tableName, rowId, "AttackCooldown", AttackPattern.AttackDuration);
            string skillName = database.GetString(tableName, rowId, "SkillToCast", "");
            if (!skillName.IsNullOrEmpty())
            {
                SkillDataAsset skillDataAsset = RpgManager.GetSkillDataAssetById(skillName);
                AttackPattern.SkillToCast = skillDataAsset;
                if (skillDataAsset != null)
                {
                    AttackPattern.AttackType = AttackTypes.SkillCast;
                }
            }
        }

        public override void UpdateFromDatabaseRow(DataTable.RowData rowData)
        {
            AttackPattern.AttackImplementationTime = rowData.GetFloatValue("AttackTime", AttackPattern.AttackImplementationTime);
            AttackPattern.AnimationTime = rowData.GetFloatValue("AnimationTime", AttackPattern.AnimationTime);
            AttackPattern.AttackDuration = rowData.GetFloatValue("AttackCooldown", AttackPattern.AttackDuration);
            string skillName = rowData.GetStringValue("SkillToCast", "");
            if (!skillName.IsNullOrEmpty())
            {
                SkillDataAsset skillDataAsset = RpgManager.GetSkillDataAssetById(skillName);
                AttackPattern.SkillToCast = skillDataAsset;
                if (skillDataAsset != null)
                {
                    AttackPattern.AttackType = AttackTypes.SkillCast;
                }
            }
        }
    }
}