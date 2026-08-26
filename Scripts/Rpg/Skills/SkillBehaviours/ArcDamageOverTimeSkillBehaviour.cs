using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.Combat;
using UnityEngine;

namespace Kuantech.Rpg.Skills
{
    /// <summary>
    /// Implemented by an FX component sitting on a channeled cone's spawned Effect that wants to know how
    /// many arcs are currently active (e.g. to clone itself into a ring around the live-tracked main
    /// instance). A Core-owned contract rather than ArcDamageOverTimeSkillBehaviour referencing a concrete
    /// HordeBonkers FX script type directly — Core must not depend on game-specific code.
    /// </summary>
    public interface IArcCountConfigurable
    {
        void Configure(int count);
    }

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
        [Tooltip("Skill variable holding how many cones to fire, evenly spread around a full circle starting " +
                 "at the live direction (e.g. 2 -> the second cone sits 180 degrees behind the first). Leave " +
                 "blank or at 1 for the original single-cone behaviour.")]
        public string ArcCountKey;

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
        private readonly List<IHittable> _hitBuffer = new List<IHittable>();
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

        /// <summary>
        /// Right after the main FX is spawned (base call), tells any IArcCountConfigurable on it how many
        /// arcs are active this cast, so a multi-arc modifier's extra cones get their own visual ring instead
        /// of only ever showing the original single cone.
        /// </summary>
        protected override void PlayBehaviourEffects()
        {
            base.PlayBehaviourEffects();
            if (BehaviourData.ConfigData is not ArcDamageOverTimeSkillBehaviourConfig config) return;

            int arcCount = GetArcCount(config);
            foreach (var effect in PlayedEffects)
            {
                if (effect == null) continue;
                IArcCountConfigurable configurable = effect.GetComponent<IArcCountConfigurable>();
                configurable?.Configure(arcCount);
            }
        }

        // ArcCountKey is optional (documented as "leave blank for single-cone") -- Skill.GetSkillVariable
        // looks the key up in a Dictionary and throws on a null key, so this must short-circuit before ever
        // calling GetSkillVariableValue with one, the same guard SkillThrowable.ReadSkillValue uses.
        private int GetArcCount(ArcDamageOverTimeSkillBehaviourConfig config)
        {
            return string.IsNullOrEmpty(config.ArcCountKey)
                ? 1
                : Mathf.Max(1, Mathf.RoundToInt(ParentSkill.GetSkillVariableValue(config.ArcCountKey, 1f)));
        }

        private void DoTick(ArcDamageOverTimeSkillBehaviourConfig config)
        {
            Actor caster = GetParentActor();
            if (caster == null) return;

            Vector3 origin = GetLiveStartPosition();
            Vector3 baseDirection = GetLiveDirection();
            float range = ParentSkill.GetSkillVariableValue(config.RangeKey);
            float damage = ParentSkill.GetSkillVariableValue(config.DamageKey);
            if (range <= 0f || damage <= 0f) return;

            int arcCount = GetArcCount(config);

            for (int i = 0; i < arcCount; i++)
            {
                // i == 0 is always exactly baseDirection (the live-aimed cone) -- only additional cones rotate
                // away from it, evenly around the full circle (count 2 -> 180 degrees apart, 3 -> 120, ...).
                Vector3 direction = i == 0
                    ? baseDirection
                    : Quaternion.AngleAxis(i * (360f / arcCount), Vector3.up) * baseDirection;
                DoTickForDirection(config, caster, origin, direction, range, damage);
            }
        }

        private void DoTickForDirection(ArcDamageOverTimeSkillBehaviourConfig config, Actor caster,
            Vector3 origin, Vector3 direction, float range, float damage)
        {
            _hitBuffer.Clear();
            _hitBuffer.AddRange(CombatUtilities.GetHittablesInArc3D(origin, direction, range, config.ArcAngle, config.TargetLayerMask));

            HitInfo hitInfo = new HitInfo
            {
                Hitter = caster.gameObject,
                DamageInfo = new DamageInfo { DamageType = config.DamageType, DamageAmount = damage },
                // A continuous cone does not shove targets around every tick — only a one-shot impact would.
                HitDirection = direction,
            };

            foreach (var target in _hitBuffer)
            {
                if (target == null || ReferenceEquals(target, caster)) continue;

                target.OnHit(hitInfo);

                // The status effect is this skill's own extra payload beyond plain damage — only makes sense
                // on an Actor with a StatusEffectHandler (a destructible crate has neither), and only on an
                // actual enemy (Actor.OnHit already rejected a non-enemy hit above, but that gives us no
                // signal back — so re-check here for this side effect specifically).
                if (target is Actor actorTarget && caster.IsEnemy(actorTarget))
                    ApplyStatusEffect(config, caster, actorTarget);
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
