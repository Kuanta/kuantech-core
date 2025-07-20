using Kuantech.Core;
using Kuantech.Core.Database;
using Kuantech.Rpg;
using UnityEngine;

namespace Kuantech.Core
{
    public class AttackPatternLoader : ActorBlueprintComponent
    {
        [Header("Keys")] 
        public string AttackTypeKey;
        public string DamageKey;
        public string AttributeScaleFactorKey;
        public string AttackRangeKey;
        public string AttackTimeKey;
        public string AnimationTimeKey;
        public string CooldownTimeKey;

        public AttributeAsset DamageScaleAttributeAsset = null;
        
        public string ProjectilePrefabIdKey;
        public string ProjectileSpeedKey;
        
        [Header("Database")]
        public string DatabaseName;
        public string TableName;

        public string ProjectilePrefabCategory = "Projectiles";
        
        public override void OnActorCreated(ActorBlueprint blueprint, Actor actor)
        {
            string actorId = actor.Id;
            KtDatabase db = KtDatabaseManager.GetDatabase(DatabaseName);
            if (db == null) return;

            // Set attack pattern
            CombatModule apm = actor.GetModule<CombatModule>();
            AttackPattern attackPattern = new AttackPattern();
            if (apm != null)
            {
                attackPattern.AttributeToScaleDamage = DamageScaleAttributeAsset;
                attackPattern.AttackType = (AttackTypes) db.GetInteger(TableName, actorId, AttackTypeKey);
                attackPattern.DamageInfo = new DamageInfo()
                {
                    DamageAmount = db.GetFloat(TableName, actorId, DamageKey),
                };
                attackPattern.Range = db.GetFloat(TableName, actorId, AttackRangeKey);
                attackPattern.AttackImplementationTime = db.GetFloat(TableName, actorId, AttackTimeKey);
                //attackPattern.AnimationTime = db.GetFloat(TableName, actorId, AnimationTimeKey);
                attackPattern.AttackDuration = db.GetFloat(TableName, actorId, CooldownTimeKey);

                attackPattern.AttributeScaleFactor = db.GetFloat(TableName, actorId, AttributeScaleFactorKey, 1f);
                
                int projectilePrefabIndex = db.GetInteger(TableName, actorId, ProjectilePrefabIdKey, 0);

                GameObject projectilePrefab = PrefabLibrary.GetPrefab(ProjectilePrefabCategory, projectilePrefabIndex);
                if (projectilePrefab != null)
                {
                    attackPattern.ProjectilePrefab = projectilePrefab.GetComponent<Projectile>();
                }
          
                apm.SetCurrentAttackPattern(attackPattern);
            } 
        }
    }
}