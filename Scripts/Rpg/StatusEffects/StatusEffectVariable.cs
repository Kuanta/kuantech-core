using System;
using Kuantech.Rpg;

namespace Kuantech.Core.Combat
{
    public struct StatusEffectVariableData
    {
        public string VariableId;
        public string VariableName;
        public float Value;
        public AttributeAsset AttributeToScaleWith;
        public float AttributrScalingFactor;
    }
    
    [Serializable]
    public class StatusEffectVariable
    {
        [NonSerialized] public StatusEffectVariableData StatusEffectVariableData;
        [NonSerialized] public StatusEffect ParentStatusEffect;

        public StatusEffectVariable(StatusEffectVariableData data)
        {
            StatusEffectVariableData = data;
        }

        public float GetValue()
        {
            // Attribute scaling is added ON TOP of the base value, not instead of it — and only when the
            // applier is still around with a stat module and an attribute to scale with.
            if (StatusEffectVariableData.AttributeToScaleWith != null &&
                ParentStatusEffect != null && ParentStatusEffect.ApplyData != null && ParentStatusEffect.ApplyData.Applier != null)
            {
                StatsModule stats = ParentStatusEffect.ApplyData.Applier.GetModule<StatsModule>();
                if (stats != null)
                {
                    float attributeValue = stats.GetAttributeValue(StatusEffectVariableData.AttributeToScaleWith);
                    return StatusEffectVariableData.Value + attributeValue * StatusEffectVariableData.AttributrScalingFactor;
                }
            }

            return StatusEffectVariableData.Value;
        }
    }
}