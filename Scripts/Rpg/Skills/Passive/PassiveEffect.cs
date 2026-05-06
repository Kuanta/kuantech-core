using System;
using Kuantech.Core;

namespace Kuantech.Rpg.Skills
{
    /// <summary>
    /// Base class for passive skill effects. Each subclass manages its own
    /// event subscriptions inside OnActivate/OnDeactivate.
    /// Instances are cloned per-actor at runtime so subscriptions never bleed
    /// across actors sharing the same PassiveSkillDataAsset.
    /// </summary>
    [Serializable]
    public abstract class PassiveEffect
    {
        public PassiveSkill ParentSkil;
        [NonSerialized] public Actor Owner;
        public virtual void OnActivate(PassiveSkill skill)
        {
            ParentSkil = skill;
            Owner = skill.ParentSpellBook.Actor;
        }
        public virtual void OnDeactivate(PassiveSkill skill) { }
        public virtual void OnUpdate(PassiveSkill skill, float deltaTime) { }

        public virtual PassiveEffect Clone() => (PassiveEffect)MemberwiseClone();
    }
}
