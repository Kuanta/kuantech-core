using System;
using Kuantech.Core;
using Kuantech.Core.Combat;
using UnityEngine;

namespace Kuantech.AI.ActionSequencer
{
    public class AttackAction : SequenceAction
    {
        [NonSerialized] public CombatModule CombatModule;
        public AttackTypes AttackType;
        public string TargetVariableName = "Target";
        public bool AttackWithoutTarget;

        private Actor _target;

        public AttackAction(AttackTypes attackType, bool attackWithoutTarget)
        {
            AttackType = attackType;
            AttackWithoutTarget = attackWithoutTarget;
        }
        public override void Initialize(GameObject parent)
        {
            base.Initialize(parent);
            CombatModule = parent.GetComponent<CombatModule>();
        }
        public override void Execute()
        {
            base.Execute();
            if (CombatModule == null)
            {
                IsComplete = true;
                return;
            }

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
            if (CombatModule.IsAttacking()) return;
            
            //Don't check target
            if (AttackWithoutTarget)
            {
                CombatModule.AttackToDirection(CombatModule.Actor.GetActorDirection());
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
                CombatModule.AttackToTarget(_target);
            }
       
        }
        
        private void OnAttackComplete()
        {
            IsComplete = true;
        }
        
    }
}