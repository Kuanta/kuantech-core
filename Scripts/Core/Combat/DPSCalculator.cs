using System;
using Kuantech.ConveyorDefense;
using Kuantech.Rpg;
using UnityEngine;

namespace Kuantech.Core.Combat
{
    [Serializable]
    public class DPSCalculator
    {
        public AttributeAsset DamageAsset;
        public AttributeAsset CriticalDamageMultiplierAsset;
        public AttributeAsset CriticlaChanceAsset;
        public AttributeAsset AttackSpeedAsset;
        public float MinAttackSpeed = 100;
        public float MaxAttackSpeed = 1000f;
        
        public float CalculateDPS(ActorBlueprint actorBlueprint, int level)
        {
            float dps = 0;
            StatsSetterComponent statsSetterComponent = actorBlueprint.GetActorBlueprintComponent<StatsSetterComponent>();
            AttackPatternSetter attackPatternSetter = actorBlueprint.GetActorBlueprintComponent<AttackPatternSetter>();

            if (statsSetterComponent == null || attackPatternSetter == null) return 0;
            AttributeDefinition damageDef = statsSetterComponent.GetAttributeDefinition(DamageAsset);
            AttributeDefinition criticalDamageDef = statsSetterComponent.GetAttributeDefinition(CriticalDamageMultiplierAsset);
            AttributeDefinition criticalChanceDef = statsSetterComponent.GetAttributeDefinition(CriticlaChanceAsset);

            float damageVal = damageDef.GetValue(level, 0); //Assuming rank is 0
            float criticalDamageMultiplier = criticalDamageDef.GetValue(level, 0); //Cr
            float criticalChanceVal = criticalChanceDef.GetValue(level, 0); //Between 0 and 1

            float baseAttackSpeed = 100;
            if (AttackSpeedAsset != null)
            {
                float attackSpeedAttrib = statsSetterComponent.GetAttributeDefinition(AttackSpeedAsset).GetValue(level, 0);
                baseAttackSpeed = Mathf.Clamp(attackSpeedAttrib, MinAttackSpeed, MaxAttackSpeed);
            }

            float baseAttack = baseAttackSpeed;
            float timeScale = CombatUtilities.GetAttackSpeedMultiplier(baseAttackSpeed,
                attackPatternSetter.AttackPattern.AttackDuration, MinAttackSpeed, MaxAttackSpeed);
            
            float attackTime = baseAttack * timeScale; //Cooldowns between each attack
            
            float attackImplementationTime = attackPatternSetter.AttackPattern.AttackImplementationTime * timeScale;
            bool continuousAttack = attackPatternSetter.AttackPattern.Continious;
            int attackCount = 1;
            if (continuousAttack)
            {
                attackCount = Mathf.FloorToInt((attackPatternSetter.AttackPattern.ContinuousAttackMaxTime * timeScale)/
                              attackImplementationTime);
            }
            
            //calculate dps using above values;
            float expectedDamage = damageVal * (1 + criticalChanceVal * (criticalDamageMultiplier - 1.0f));
            dps = expectedDamage * attackCount / attackTime;
            return dps;
        }
    }
}