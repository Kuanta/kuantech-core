using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.Combat;
using UnityEngine;

namespace Kuantech.Rpg.Skills
{
    public class ArcDamageOverTimeSkillBehaviourConfig : SkillBehaviourConfigData
    {
        [Header("Arc")]
        [Tooltip("Layers a target must be on to be considered — a coarse physics prefilter. Faction is " +
                 "checked separately (only the caster's enemies are ever hit), so 'Everything' is safe here.")]
        public LayerMask TargetLayerMask = ~0;
        [Tooltip("Total angle of the cone, in degrees.")]
        public float ArcAngle = 60f;
        [Tooltip("Seconds between damage ticks. How long the channel itself lasts is the behaviour's own " +
                 "Duration, not this.")]
        public float TickInterval = 0.25f;
        public DamageType DamageType;

        [Header("Variable Keys")]
        public string RangeKey = "Range";
        public string DamageKey = "Damage";

        [Header("Status Effect (optional)")]
        [Tooltip("Applied to every actor a tick hits — e.g. Burn for a flamethrower, Freeze for a cryo " +
                 "cone. Leave empty for a cone that only deals direct damage.")]
        public StatusEffectAsset StatusEffectToApply;
        [Tooltip("Skill variable holding the status effect's own duration, in seconds. Ignored if " +
                 "StatusEffectToApply is empty.")]
        public string StatusEffectDurationKey;
        [Tooltip("Skill variable holding the seconds between the status effect's own ticks. Ignored if " +
                 "StatusEffectToApply is empty.")]
        public string StatusEffectTickIntervalKey;
        [Tooltip("Skill variable holding the status effect's per-tick amount (DamageOverTimeStatusEffect " +
                 "only). Leave empty to use the effect asset's own authored value instead of overriding it.")]
        public string StatusEffectDamageKey;
    }

    /// <summary>
    /// A channeled cone: every TickInterval seconds for as long as the behaviour's Duration lasts, damages
    /// every enemy currently standing in an arc in front of the caster, optionally applying a status effect
    /// to each (a flamethrower's burn, a frost cone's freeze — same behaviour, different config/asset).
    ///
    /// The cone's direction is re-read live on every tick (GetLiveDirection), not frozen at cast time — an
    /// auto-cast target that walks sideways drags the flame with it instead of leaving it pointing at empty
    /// air where the target used to be.
    /// </summary>
    public class ArcDamageOverTimeSkillBehaviour : SkillBehaviour
    {
        // Reused across ticks so a channel with many enemies in range doesn't allocate a fresh list per tick.
        private readonly List<Actor> _hitBuffer = new List<Actor>();
        private float _nextTickTime;

        protected override void OnBehaviourStarted()
        {
            base.OnBehaviourStarted();
            _nextTickTime = 0f; // first tick lands as soon as the cast resolves (CastTime elapses)
        }

        protected override void OnBehaviourUpdate(float elapsedTime)
        {
            base.OnBehaviourUpdate(elapsedTime);
            if (elapsedTime < _nextTickTime) return;
            if (BehaviourData.ConfigData is not ArcDamageOverTimeSkillBehaviourConfig config) return;

            _nextTickTime = elapsedTime + Mathf.Max(0.05f, config.TickInterval);
            DoTick(config);
        }

        private void DoTick(ArcDamageOverTimeSkillBehaviourConfig config)
        {
            Actor caster = GetParentActor();
            if (caster == null) return;

            Vector3 origin = GetLiveStartPosition();
            Vector3 direction = GetLiveDirection();
            float range = ParentSkill.GetSkillVariableValue(config.RangeKey);
            float damage = ParentSkill.GetSkillVariableValue(config.DamageKey);
            if (range <= 0f || damage <= 0f) return;

            _hitBuffer.Clear();
            _hitBuffer.AddRange(CombatUtilities.GetActorsInArc3D(origin, direction, range, config.ArcAngle, config.TargetLayerMask));

            foreach (var target in _hitBuffer)
            {
                if (target == null || target == caster || !target.IsAlive()) continue;
                if (!caster.IsEnemy(target)) continue;

                target.OnHit(new HitInfo
                {
                    Hitter = caster.gameObject,
                    DamageInfo = new DamageInfo { DamageType = config.DamageType, DamageAmount = damage },
                    // A continuous cone does not shove targets around every tick — only a one-shot impact would.
                    HitDirection = direction,
                });

                ApplyStatusEffect(config, caster, target);
            }

         
        }

        private void ApplyStatusEffect(ArcDamageOverTimeSkillBehaviourConfig config, Actor caster, Actor target)
        {
            if (config.StatusEffectToApply == null) return;
            StatusEffectHandler handler = target.GetModule<StatusEffectHandler>();
            if (handler == null) return;

            StatusEffectApplyData applyData = new StatusEffectApplyData
            {
                Applier = caster,
                Duration = ParentSkill.GetSkillVariableValue(config.StatusEffectDurationKey),
                TickPeriod = ParentSkill.GetSkillVariableValue(config.StatusEffectTickIntervalKey),
            };

            StatusEffect effect = config.StatusEffectToApply.CreateStatusEffect(applyData);
            if (effect == null) return;

            // Same reasoning as MolotovThrowable: the asset's own variable is shared by every application,
            // so this tick's damage overrides a copy rather than writing into it.
            if (!string.IsNullOrEmpty(config.StatusEffectDamageKey))
            {
                float statusDamage = ParentSkill.GetSkillVariableValue(config.StatusEffectDamageKey);
                effect.SetVariableOverride(DamageOverTimeStatusEffect.DamagePerTickKey, statusDamage);
            }

            handler.AddStatusEffect(effect);
        }
    }
}
