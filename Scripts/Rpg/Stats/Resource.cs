using System;
using Kuantech.Core.Combat;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.Serialization;

namespace Kuantech.Rpg
{

    [Serializable]
    public struct SerializableResourceDefinition
    {
        public string ResourceAssetId;
        [Tooltip("Attribute that dictates the max value")] public string MaxValueAttributeId;
        [Tooltip("Attribute that dictates regeneration value")] public string RegenValueAttributeId;
        public float DefaultMaxValue;
        public float RegenValue;
    }

    [Serializable]
    public struct ResourceDefinition
    {
        public ResourceAsset ResourceAsset;
        [Tooltip("Attribute that dictates the max value")] public AttributeAsset MaxValueAttribute;
        [Tooltip("Attribute that dictates regeneration value")] public AttributeAsset RegenValueAttribute;
        public float DefaultMaxValue;
        public float RegenValue;
    }
    
    public class Resource
    {
        public ResourceAsset ResourceAsset;
        public AttributeAsset MaxValueAttributeAsset; //Which attribute this resource is based on. Defines the max value
        public AttributeAsset RegenAttributeAsset;
        public float DefaultMaxValue; //If attribute asset is null, use this as max value
        public float DefaultRegenValue;
        
        public StatsModule StatsModule;

        public void ApplyResourceDefinition(ResourceDefinition definition)
        {
            ResourceAsset = definition.ResourceAsset;
            MaxValueAttributeAsset = definition.MaxValueAttribute;
            RegenAttributeAsset = definition.RegenValueAttribute;
            DefaultMaxValue = definition.DefaultMaxValue;
            DefaultRegenValue = definition.RegenValue;
        }
        
        //Runtime
        public float CurrentValue;
        
        public void SetValue(float value)
        {
            CurrentValue = ClampValue(value);

            //TODO: refactor code so that it isn't this ugly
            //Update resource bar.      
            if(StatsModule != null)
            {
                var hm = StatsModule.Actor.GetModule<HealthcareModule>();   
                if(hm != null && ResourceAsset != null)
                {
                    hm.UpdateResourceBar(ResourceAsset);
                }
            }
        }

        public void AddValue(float value)
        {
            SetValue(CurrentValue + value);
        }

        public float ClampValue(float value)
        {
            return Mathf.Clamp(value, 0, GetMaxValue());
        }
        
        /// <summary>
        /// Returns the max value
        /// </summary>
        /// <returns></returns>
        public float GetMaxValue()
        {
            if (MaxValueAttributeAsset == null) return DefaultMaxValue;
            if (StatsModule == null) return DefaultMaxValue;
            Attribute attribute = StatsModule.GetAttribute(MaxValueAttributeAsset);
            if (attribute == null) return DefaultMaxValue;
            return StatsModule.GetAttributeValue(MaxValueAttributeAsset);
        }
        
        /// <summary>
        /// Returns the regeneration value of the resource.
        /// </summary>
        /// <returns></returns>
        public float GetRegenValue()
        {
            if (RegenAttributeAsset == null || StatsModule == null) return DefaultRegenValue;
            Attribute attribute = StatsModule.GetAttribute(RegenAttributeAsset);
            if (attribute == null) return DefaultRegenValue;
            return StatsModule.GetAttributeValue(RegenAttributeAsset);
        }
        
        /// <summary>
        /// Adds the regeneration value
        /// </summary>
        /// <param name="tickTime"></param>
        public void RegenTick(float tickTime)
        {
            float regenPerSec = GetRegenValue();
            AddValue(regenPerSec * tickTime);
        }
        
        public float GetValue()
        {
            return CurrentValue;
        }
        
        /// <summary>
        /// Sets the current value to maximum value
        /// </summary>
        public void RefreshValue()
        {
            SetValue(GetMaxValue());
        }
    }
}