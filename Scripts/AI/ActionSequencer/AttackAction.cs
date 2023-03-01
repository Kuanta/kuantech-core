using Kuantech.Core;

namespace Kuantech.ActionSequencer
{
    public class AttackAction : SequenceAction
    {
        public CombatModule CombatModule;
        public AttackTypes AttackType;
        public string TargetVariableName = "Target";
        public bool AttackWithoutTarget;

        private Actor _target;
        public override void Execute()
        {
            base.Execute();
            if (CombatModule == null)
            {
                IsComplete = true;
                return;
            }
            CombatModule.SetAttackType(AttackType);

            if (Sequencer == null)
            {
                _target = null;
            }

            if (Sequencer != null)
            {
                _target = Sequencer.VariableTable.GetVariable<Actor>(TargetVariableName);
            }
        }

        public override void Update(float deltaTime)
        {
            if (CombatModule.IsAttacking) return;
            
            //Don't check target
            if (AttackWithoutTarget)
            {
                CombatModule.Attack(true, OnAttackComplete);
                return;
            }  
            
            if (_target == null)
            {
                //Shouldn't attack into emptiness
                IsComplete = true;
                return;
            }
            
            if (CombatModule.IsInAttackRange(_target.transform))
            {
                CombatModule.Attack(true, OnAttackComplete);
            }
       
        }
        
        private void OnAttackComplete()
        {
            IsComplete = true;
        }
        
    }
}