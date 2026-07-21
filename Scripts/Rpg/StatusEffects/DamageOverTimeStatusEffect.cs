using System;
using Kuantech.Rpg;
using UnityEngine;

namespace Kuantech.Core.Combat
{
    [Serializable]
    public class DamageOverTimeStatusEffectConfig : StatusEffectConfig
    {
        [Tooltip("Damage type each tick deals — drives resistances and which resource is hit.")]
        public DamageType DamageType;
    }

    /// <summary>
    /// Deals damage every tick for as long as it lasts — burning, poison, bleed. The per-tick amount comes
    /// from a status effect variable, so it scales with the applier's attributes; tick period and duration
    /// come from the apply data, so the same asset can burn briefly and fiercely or slowly and long.
    ///
    /// Damage is attributed to the applier, so kills still credit whoever threw the molotov.
    /// </summary>
    public class DamageOverTimeStatusEffect : StatusEffect
    {
        /// <summary>Variable id holding the damage dealt on each tick.</summary>
        public const string DamagePerTickKey = "DamagePerTick";

        public override void OnTick()
        {
            base.OnTick();
            if (Target == null || !Target.IsAlive()) return;

            float damagePerTick = GetVariable(DamagePerTickKey);
            if (damagePerTick <= 0f) return;

            Actor applier = ApplyData != null ? ApplyData.Applier : null;
            DamageType damageType = StatusEffectAsset != null && StatusEffectAsset.Config is DamageOverTimeStatusEffectConfig config
                ? config.DamageType
                : null;

            Target.OnHit(new HitInfo
            {
                // Applier can be gone by now (dead, despawned) — the tick still lands, just unattributed.
                Hitter = applier != null ? applier.gameObject : null,
                DamageInfo = new DamageInfo
                {
                    DamageType = damageType,
                    DamageAmount = damagePerTick,
                },
                // A burn does not shove the body around; only the initial blast does that.
                HitDirection = Vector3.zero,
                KnockbackForce = 0f,
                KnockbackDuration = 0f,
            });
        }
    }
}
