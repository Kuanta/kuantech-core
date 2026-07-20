using System;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.Rpg.Skills
{
    [Serializable]
    public class ThrowSkillBehaviourConfig : SkillBehaviourConfigData
    {
        [Tooltip("Throwable to lob. Its damage/radius/knockback come from this skill's variables (see the " +
                 "key fields on the prefab); everything else — layers, damage type, arc, FX — is prefab-authored.")]
        public SkillThrowable ThrowablePrefab;
    }

    /// <summary>
    /// Lobs a <see cref="SkillThrowable"/> at the cast target and lets it resolve on landing. One behaviour
    /// class covers every throwable — a molotov and a grenade differ by prefab (and by the throwable
    /// subclass's payload), not by another behaviour type.
    /// </summary>
    public class ThrowSkillBehaviour : SkillBehaviour
    {
        protected override void BehaviourImplementation()
        {
            if (BehaviourData.ConfigData is not ThrowSkillBehaviourConfig config) return;
            if (config.ThrowablePrefab == null) return;

            Actor caster = GetParentActor();
            if (caster == null) return;

            SkillThrowable throwable = PoolManager.GetObjectFromPool(config.ThrowablePrefab);
            if (throwable == null) return;

            throwable.ThrowFromSkill(ParentSkill, caster, GetLiveStartPosition(), GetTargetPoint());
        }

        /// <summary>
        /// Where the throwable should land. Aims at the target actor's feet when there is one; otherwise at
        /// the frozen aim point, or as a last resort straight ahead at the edge of the skill's range.
        /// Note this point is frozen at throw time — a target that walks away will not be followed, which
        /// is the intended feel for a lobbed weapon.
        /// </summary>
        protected virtual Vector3 GetTargetPoint()
        {
            if (CurrentSkillCastData.Target != null) return CurrentSkillCastData.Target.GetActorLocation();
            if (CurrentSkillCastData.TargetPosition.sqrMagnitude > 0f) return CurrentSkillCastData.TargetPosition;
            return GetLiveStartPosition() + GetLiveDirection() * ParentSkill.GetRange();
        }
    }
}
