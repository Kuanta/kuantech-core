using Kuantech.Core;

namespace Kuantech.Rpg.Skills
{
    /// <summary>
    /// Family of classes that can check if the skill can be cast 
    /// </summary>
    public abstract class SkillCastChecker
    {
        public abstract bool CanBeCast(Skill skill, ActionCastData castData);
    }
}