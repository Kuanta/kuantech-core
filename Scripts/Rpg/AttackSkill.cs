using System;
using Kuantech.Combat;
using UnityEngine;

namespace Kuantech.Core.Rpg
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
        public float RankMultiplier;
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
            return rank * RankMultiplier + BaseValue;
        }
    }

    
    /// <summary>
    /// Attack skills are passive effects that are triggered on certain points during the lifetime of the combat modules
    /// </summary>
    public class AttackSkill : Skill
    {
        public int Rank = 1;
        protected CombatModule CombatModule;
        
        //Channel Casts
        public bool IsChanneled = false;
        public bool IsBeingCast = false;
        
        private float _castStartTime = 0f;
        protected float LastTickTime = 0f;
        
        //Common SkillVariables
        public SkillVariable Damage;
        protected SkillVariable ChannelDuration;
        protected SkillVariable ChannelTickRate;
        public SkillVariable Knockback;
        public SkillVariable KnockbackTime;
        public SkillVariable Range;
        public SkillVariable Speed;
        public SkillVariable ProjectileId;
        
        protected AttackSkill(SkillData data) : base(data)
        {
            //Get Common skill variables 
            ChannelDuration = GetSkillVariable("channelDuration");
            ChannelTickRate = GetSkillVariable("channelTickRate");
            Damage = GetSkillVariable("damage");
            Knockback = GetSkillVariable("knockback");
            KnockbackTime = GetSkillVariable("knockbackTime");
            Range = GetSkillVariable("range");
            ProjectileId = GetSkillVariable("projectileId");
            Speed = GetSkillVariable("speed");
        }
        
        public SkillVariable GetSkillVariable(string variableName)
        {
            if (SkillVariables.ContainsKey(variableName)) return SkillVariables[variableName];
            return new SkillVariable()
            {
                Name = null,
                BaseStat = StatTypes.None,
                BaseValue = 0,
                StatMultiplier = 0,
                RankMultiplier = 0,
            };
        }
        public virtual void Initialize(CombatModule combatModule)
        {
            CombatModule = combatModule;
            CombatModule.AttackEvent += OnAttack;
            CombatModule.MeleeImpactEvent += OnMeleeImpact;
            CombatModule.ProjectileShotEvent += OnProjectileShot;
            CombatModule.RangedImpactEvent += OnRangedImpact;
            IsBeingCast = false;
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
            Cancel();
        }

        public override bool Cast(Actor caster)
        {
            if (!base.Cast(caster) || (IsBeingCast && IsChanneled)) return false;
            //Calculate
            float energyCost = GetEnergyCost();

            if (CombatModule.Actor.Energy < energyCost) return false;
            if (CombatModule.IsAttacking)
            {
                CombatModule.Cancel();
            }
            CombatModule.Actor.Energy -= energyCost;

            _castStartTime = Time.time;
            if (IsChanneled) IsBeingCast = true;
            
            //Apply global cooldown
            CombatModule.GlobalCooldown.StartCooldown(IsChanneled
                ? ChannelDuration.GetValue(Rank)
                : Config.GLOBAL_COOLDOWN_TIME);
            
            return true;
        }

        public virtual void Update(float deltaTime)
        {
            if (!IsBeingCast) return;
            if (Time.time - _castStartTime > ChannelDuration.GetValue())
            {
                IsBeingCast = false;
            }

            if (!(Time.time - LastTickTime > ChannelTickRate.GetValue())) return;
            LastTickTime = Time.time;
                
            //Tick
            Tick();
        }

        protected virtual void Tick()
        {
            
        }
        /// <summary>
        /// Virtual method for skill canceling. Called when skill is removed or owner is dead
        /// </summary>
        /// <returns></returns>
        public virtual void Cancel()
        {
            IsBeingCast = false;
        }
        
        #region Common Skill Cast Components

        protected Projectile ShootProjectile()
        {
            GameObject projectilePrefab = Librarian.Instance.GetProjectilePrefab((int) ProjectileId.GetValue());
            Projectile shotProjectile = CombatModule.ShootProjectile(CombatModule.EquippedWeapon, 
                projectilePrefab, 
                CombatModule.GetShootPosition(),
                CombatModule.transform.forward,false);
            if (shotProjectile == null) return null;
            shotProjectile.DestroyOnImpact = true;
            shotProjectile.Range = Range.GetValue(Rank);
            shotProjectile.Speed = Speed.GetValue(Rank);
            shotProjectile.Knockback = Knockback.GetValue(Rank);
            shotProjectile.KnockbackTime = KnockbackTime.GetValue(Rank);
            return shotProjectile;
        }
        #endregion
    }
}