using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core.Combat
{

    [CreateAssetMenu(fileName = "Skill", menuName = "Kuantech/Combat/Skill", order = 0)]
    public class Skill : ScriptableObject {
        public List<CombatResourceData> CombatResources;
        public float CastTime;
        public bool IsChanneled;

        [Header("Actions")]
        public List<SkillAction> PreCastActions;
        public List<SkillAction> CastActions;
        public List<SkillAction> PostCastActions;

        public virtual void PreCast(CombatModule cm)
        {
            foreach(var action in PreCastActions)
            {
                action.Act(cm);
            }
        }

        public virtual void Cast(CombatModule cm)
        {
            foreach (var action in CastActions)
            {
                action.Act(cm);
            }
        }

        public virtual void PostCast(CombatModule cm)
        {
            foreach (var action in PostCastActions)
            {
                action.Act(cm);
            }
        }

    }
}