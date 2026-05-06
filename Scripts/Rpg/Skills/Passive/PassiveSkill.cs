using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Rpg.Skills
{
    public class PassiveSkillVariable
    {
        public SkillVariableData Data;
        public PassiveSkill ParentSkill;

        public PassiveSkillVariable(SkillVariableData data) { Data = data; }

        public float GetValueByRank(int rank)
        {
            float attr = 0f;
            if (Data.AttributeToScaleWith != null && ParentSkill?.ParentSpellBook?.Actor != null)
            {
                StatsModule sm = ParentSkill.ParentSpellBook.Actor.GetModule<StatsModule>();
                if (sm != null) attr = sm.GetAttributeValue(Data.AttributeToScaleWith);
            }
            return Data.BaseValue + Data.ValuePerRank * rank + Data.AttributeScalingFactor * attr;
        }
    }

    public class PassiveSkill
    {
        public PassiveSkillDataAsset DataAsset;
        public SpellBook ParentSpellBook;
        public int Rank;

        private readonly List<PassiveEffect> _effects = new();
        private readonly Dictionary<string, PassiveSkillVariable> _variables = new();
        private float _lastProcTime = float.MinValue;

        public void Initialize(SpellBook spellBook, PassiveSkillDataAsset dataAsset)
        {
            DataAsset       = dataAsset;
            ParentSpellBook = spellBook;

            // Clone effects so each actor has its own instances — prevents
            // event subscriptions from bleeding across actors sharing the same SO.
            _effects.Clear();
            foreach (var effect in dataAsset.Effects)
                if (effect != null) _effects.Add(effect.Clone());

            foreach (var vd in dataAsset.SkillVariableDatas)
                _variables[vd.VariableId] = new PassiveSkillVariable(vd) { ParentSkill = this };
        }

        public void Activate()
        {
            foreach (var effect in _effects)
                effect.OnActivate(this);
        }

        public void Deactivate()
        {
            foreach (var effect in _effects)
                effect.OnDeactivate(this);
        }

        public void Update(float deltaTime)
        {
            foreach (var effect in _effects)
                effect.OnUpdate(this, deltaTime);
        }

        /// <summary>
        /// Returns true if the proc passed both cooldown and chance rolls.
        /// Effects call this inside their own event handlers.
        /// </summary>
        public bool TryProc()
        {
            if (DataAsset.ProcCooldown > 0f && Time.time - _lastProcTime < DataAsset.ProcCooldown)
                return false;
            if (DataAsset.ProcChance < 1f && Random.value > DataAsset.ProcChance)
                return false;
            _lastProcTime = Time.time;
            return true;
        }

        public float GetVariableValue(string variableId, float defaultValue = 0f)
        {
            if (_variables.TryGetValue(variableId, out var v))
                return v.GetValueByRank(Rank);
            return defaultValue;
        }

        public string GetId() => DataAsset?.SkillId ?? string.Empty;
    }
}
