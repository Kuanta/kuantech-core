using System;
using Kuantech.Core.Combat;
using Kuantech.Rpg;
using UnityEngine;

namespace Kuantech.ActionSequencer
{
    public class AttackAction : SequenceAction
    {
        [NonSerialized] public RpgCombatModule CombatModule;
        public AttackTypes AttackType;
        public string TargetVariableName = "Target";
        public bool AttackWithoutTarget;

        private RpgActor _target;

        public AttackAction(AttackTypes attackType, bool attackWithoutTarget)
        {
            AttackType = attackType;
            AttackWithoutTarget = attackWithoutTarget;
        }
        public override void Initialize(GameObject parent)
        {
            base.Initialize(parent);
            CombatModule = parent.GetComponent<RpgCombatModule>();
        }
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
                _target = Sequencer.VariableTable.GetVariable<RpgActor>(TargetVariableName);
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