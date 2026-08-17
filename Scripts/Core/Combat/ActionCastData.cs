using System;
using UnityEngine;

namespace Kuantech.Core
{
    [Serializable]

    public class ActionCastData
    {
        public Actor Caster;
        public Vector3 StartPosition; //Start position of the cast
        public Vector3 Direction; //Direction of the cast
        public Vector3 TargetPosition; //Targeted position
        public Actor Target; //Targeted actor
        public bool OverrideRotation = true;

        /// <summary>
        /// Optional: re-evaluated every tick by a channeled behaviour (see SkillBehaviour.GetLiveDirection)
        /// to aim at whatever "the current target" means to whoever built this cast data — a player's live
        /// closest-enemy, a cursor position, anything. Lets each caster (AutoCastModule, manual test-cast,
        /// ...) define that meaning itself, without ActionCastData/SkillBehaviour knowing about any of them.
        /// Leave null to fall back to the frozen Target/TargetPosition, same as before this existed.
        /// </summary>
        public Func<Vector3> LiveAimPointProvider;

        public Vector3 GetCastPoint()
        {
            if (Target != null) return Target.transform.position;
            return TargetPosition;
        }
    }
}