using Kuantech.Core;

namespace Kuantech.ActionSequencer
{
    public class AttackAction : SequenceAction
    {
        public CombatModule CombatModule;
        public AttackTypes AttackType;
        
        public override void Execute()
        {
            base.Execute();
            if (CombatModule == null)
            {
                IsComplete = true;
                return;
            }
            CombatModule.SetAttackType(AttackType);
            CombatModule.Attack(() =>
            {
                OnAttackComplete();
            });
        }
        
        private void OnAttackComplete()
        {
            IsComplete = true;
        }
        
    }
}