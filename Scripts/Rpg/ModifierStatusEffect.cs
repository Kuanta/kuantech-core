
namespace Kuantech.Rpg
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
            Target.Stats.AddModifier(Modifier);
        }
        
        public override void OnRemove()
        {
            base.OnRemove();
            if (Modifier == null) return;
            Target.Stats.RemoveModifier(Modifier);
        }
    }
}