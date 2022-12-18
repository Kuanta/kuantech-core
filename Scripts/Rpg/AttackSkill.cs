using System;
using System.Collections.Generic;
using Kuantech.Core;

namespace Kuantech
{
    /// <summary>
    /// Skill variable that can be skilled by a stat
    /// </summary>
    [Serializable]
    public struct SkillVariable
    {
        public string Name;
        public StatTypes BaseStat;
        public float BaseValue;
        public float StatMultiplier;
        [NonSerialized] public Func<float> RankCalculation;
        public float GetValue(int rank = 0, Actor actor = null)
        {
            float baseValue = 0;
            baseValue = RankCalculation?.Invoke() ?? DefaultRankCalculation(rank);
            if (BaseStat == StatTypes.None || actor == null) return baseValue;
            return actor.Stats.GetStat(BaseStat) * StatMultiplier + BaseValue;
        }
        public float DefaultRankCalculation(int rank)
        {
            return rank * BaseValue;
        }
    }

    [Serializable]
    public struct AttackSkillData
    {
        public int Id;
        public string Name;
        public string Description;
        public int IconId;
        public float BaseEnergyCost;
        public List<SkillVariable> SkillVariables;
    }
    
    /// <summary>
    /// Attack skills are passive effects that are triggered on certain points during the lifetime of the combat modules
    /// </summary>
    public class AttackSkill
    {
        public int Id;
        public int Rank = 1;
        protected CombatModule CombatModule;
        protected Dictionary<string, SkillVariable> SkillVariables = new Dictionary<string, SkillVariable>();
        protected AttackSkillData SkillData;
        public AttackSkill(AttackSkillData data)
        {
            SkillData = data;
            Id = data.Id;
            foreach (var skillVariable in SkillData.SkillVariables)
            {
                SkillVariables[skillVariable.Name] = skillVariable;
            }
        }
        
        public virtual void Initialize(CombatModule combatModule)
        {
            CombatModule = combatModule;
            CombatModule.AttackEvent += OnAttack;
            CombatModule.MeleeImpactEvent += OnMeleeImpact;
            CombatModule.ProjectileShotEvent += OnProjectileShot;
            CombatModule.RangedImpactEvent += OnRangedImpact;
        }

        public virtual float GetEnergyCost()
        {
            return SkillData.BaseEnergyCost * Rank;
        }

        protected virtual void OnProjectileShot(object sender, ProjecitleShotInfo shotInfo){}
        protected virtual void OnMeleeImpact(object sender, Actor impacted){}
        protected virtual void OnRangedImpact(object sender, ProjectileImpactInfo impactInfo){}
        protected virtual void OnAttack(object sender, int attackIndex){}

        public void IncreaseRank()
        {
            Rank += 1;
            CombatModule.CalculateManaCosts();
        }
        public void Remove()
        {
            CombatModule.AttackEvent -= OnAttack;
            CombatModule.MeleeImpactEvent -= OnMeleeImpact;
            CombatModule.ProjectileShotEvent -= OnProjectileShot;
            CombatModule.RangedImpactEvent -= OnRangedImpact;
        }
    }
}