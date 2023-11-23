using UnityEngine;

namespace Kuantech.Rpg
{
    /// <summary>
    /// Attack skills are passive effects that are triggered on certain points during the lifetime of the combat modules
    /// </summary>
    public class AttackSkill : Skill
    {
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
        
        protected virtual void OnProjectileShot(object sender, ProjecitleShotInfo shotInfo){}
        protected virtual void OnMeleeImpact(object sender, RpgActor impacted){}
        protected virtual void OnRangedImpact(object sender, ProjectileImpactInfo impactInfo){}
        protected virtual void OnAttack(object sender, int attackIndex){}
        
        public override void AddToActor(CombatModule combatModule)
        {
            base.AddToActor(combatModule);
            CombatModule = combatModule;
            CombatModule.AttackEvent += OnAttack;
            CombatModule.MeleeImpactEvent += OnMeleeImpact;
            CombatModule.ProjectileShotEvent += OnProjectileShot;
            CombatModule.RangedImpactEvent += OnRangedImpact;
            IsBeingCast = false;
        }
     
        public override void RemoveFromActor()
        {
            base.RemoveFromActor();
            CombatModule.AttackEvent -= OnAttack;
            CombatModule.MeleeImpactEvent -= OnMeleeImpact;
            CombatModule.ProjectileShotEvent -= OnProjectileShot;
            CombatModule.RangedImpactEvent -= OnRangedImpact;
            Cancel();
        }

        public override bool Cast(RpgActor caster)
        {
            //Calculate
            float energyCost = GetEnergyCost();
            if (CombatModule.Actor.Energy < energyCost) return false;
            
            if (!base.Cast(caster) || (IsBeingCast && IsChanneled)) return false;
            
            if (CombatModule.IsAttacking)
            {
                CombatModule.Cancel();
            }
            CombatModule.Actor.SpendEnergy(energyCost);

            _castStartTime = Time.time;
            if (IsChanneled) IsBeingCast = true;
            
            //Apply global cooldown
            float globalCooldownTime = Mathf.Max(RpgConfig.GLOBAL_COOLDOWN_TIME, SkillData.AnimationTime);
            CombatModule.GlobalCooldown.StartCooldown(IsChanneled
                ? ChannelDuration.GetValue(Rank)
                : globalCooldownTime);
            
            return true;
        }

        protected override void ApplySkillEffect()
        {
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
            shotProjectile.Damage = Damage.GetValue(Rank, CombatModule.Actor);
            shotProjectile.Range = Range.GetValue(Rank);
            shotProjectile.Speed = Speed.GetValue(Rank);
            shotProjectile.Knockback = Knockback.GetValue(Rank);
            shotProjectile.KnockbackTime = KnockbackTime.GetValue(Rank);
            return shotProjectile;
        }
        #endregion
    }
}