using Kuantech.Rpg;

namespace Kuantech.Core.Combat
{
    public class ModifierStatusEffect : StatusEffect
    {
        public StatModifier Modifier;

        public ModifierStatusEffect(StatModifier modifier)
        {
            Modifier = modifier;
        }
        public override void OnAdd()
        {
            base.OnAdd();
            if (Modifier == null) return;
            Target.Actor.GetModule<StatsModule>().AddModifier(Modifier);
        }
        
        public override void OnRemove()
        {
            base.OnRemove();
            if (Modifier == null) return;
            StatsModule statModule = Target.Actor.GetModule<StatsModule>();
            statModule.RemoveModifier(Modifier);
        }
    }
}