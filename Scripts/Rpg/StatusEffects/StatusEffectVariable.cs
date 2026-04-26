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
            //try to get attribute value
            if (ParentStatusEffect != null && ParentStatusEffect.ApplyData != null && ParentStatusEffect.ApplyData.Applier != null)
            {
                Actor applier = ParentStatusEffect.ApplyData.Applier;
                float attributeValue = applier.GetModule<StatsModule>().GetAttributeValue(StatusEffectVariableData.AttributeToScaleWith);
                attributeValue *= StatusEffectVariableData.AttributrScalingFactor;
                return attributeValue;
            }

            return StatusEffectVariableData.Value;
        }
    }
}