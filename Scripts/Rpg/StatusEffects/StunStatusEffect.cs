using System;
using Kuantech.Rpg.Skills;

namespace Kuantech.Core.Combat
{
    [Serializable]
    public class StunStatusEffect : StatusEffect
    {
        private MovementModule _movementModule;
        private CombatModule _combatModule;
        private SpellBook _spellBook;
        
        public override void Initialize(StatusEffectAsset statusEffectAsset, StatusEffectData applyData)
        {
            base.Initialize(statusEffectAsset, applyData);

        }
        
        public override void OnAdd(Actor targetActor)
        {
            base.OnAdd(targetActor);
            if(Target == null)
            {
                return;
            }
            _movementModule = Target.GetModule<MovementModule>();
            _combatModule = Target.GetModule<CombatModule>();
            _spellBook = Target.GetModule<SpellBook>();
      
            if (_movementModule != null)
            {
                _movementModule.Lock(this);
            }

            if (_combatModule != null)
            {
                _combatModule.AttackLock.Lock(this);
            }

            if (_spellBook != null)
            {
                _spellBook.SkillLock.Lock(this);
            }
        }
        
        public override void OnRemove()
        {
            base.OnRemove();
            if (_movementModule != null)
            {
                _movementModule.Unlock(this);
            }

            if (_combatModule != null)
            {
                _combatModule.AttackLock.Unlock(this);
            }

            if (_spellBook != null)
            {
                _spellBook.SkillLock.Unlock(this);
            }
        }
        
        
    }
}