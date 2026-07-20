using System;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.Rpg.Skills
{
    /// <summary>
    /// A <see cref="Throwable"/> whose numbers come from the skill that threw it, so they scale with the
    /// skill's rank (and with the caster's attributes) instead of being baked into the prefab.
    ///
    /// Values are read ONCE at throw time and cached, never while the blast resolves. Two reasons: the
    /// caster can die mid-flight (skill variables walk through ParentSpellBook.Actor to read attributes),
    /// and a blast catching twenty enemies should not do a dictionary lookup per enemy.
    ///
    /// Subclass this to add a payload with its own scaling values — see <see cref="ReadSkillValues"/>.
    /// </summary>
    public class SkillThrowable : Throwable
    {
        [Header("Skill Variable Keys")]
        [Tooltip("Skill variable holding the impact damage. Leave empty to keep the value set on this prefab.")]
        public string DamageKey;
        [Tooltip("Skill variable holding the blast radius. Leave empty to keep the value set on this prefab.")]
        public string RadiusKey;
        [Tooltip("Skill variable holding the knockback force. Leave empty to keep the value set on this prefab.")]
        public string KnockbackKey;

        [NonSerialized] public Skill ParentSkill;

        /// <summary>
        /// Throws this on behalf of a skill: binds the skill, snapshots its values, then flies as usual.
        /// </summary>
        public void ThrowFromSkill(Skill skill, Actor thrownBy, Vector3 startPosition, Vector3 targetPoint)
        {
            ParentSkill = skill;
            ReadSkillValues();
            Throw(thrownBy, startPosition, targetPoint);
        }

        /// <summary>
        /// Pulls this throwable's numbers off the skill. Override to also read the values your payload
        /// needs (burn duration, tick interval, ...) into your own fields — call base first.
        /// </summary>
        protected virtual void ReadSkillValues()
        {
            Damage.DamageAmount = ReadSkillValue(DamageKey, Damage.DamageAmount);
            ImpactRadius        = ReadSkillValue(RadiusKey, ImpactRadius);
            Knockback           = ReadSkillValue(KnockbackKey, Knockback);
        }

        /// <summary>
        /// Reads one skill variable, falling back to the value already on this instance when the key is
        /// blank or the skill has no such variable — so a prefab-authored value is never silently zeroed.
        /// </summary>
        protected float ReadSkillValue(string variableKey, float fallback)
        {
            if (ParentSkill == null || string.IsNullOrEmpty(variableKey)) return fallback;
            return ParentSkill.GetSkillVariableValue(variableKey, fallback);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ParentSkill = null;
        }
    }
}
