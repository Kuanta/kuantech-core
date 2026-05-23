using Kuantech.Core;
using Kuantech.Core.Database.Attributes;
using Kuantech.Rpg.Managers;

namespace Kuantech.Rpg
{
    public class AttributeAffix : Affix
    {
        [KtDatabaseVariable("AttributeId")] public string AttributeId { get; private set; }
        [KtDatabaseVariable("MinBaseValue")] public float MinBaseValue { get; private set; }
        [KtDatabaseVariable("MaxBaseValue")] public float MaxBaseValue { get; private set; }
        [KtDatabaseVariable("ValuePerLevel")] public float ValuePerLevel { get; private set; }
        public float[] RarityScales {get; private set;}
        private StatModifier _addedModifier;
        public override void ApplyAffixToActor(Actor actor)
        {
            //Add modifier
            StatModifierData data = GetStatModifierData();
            StatsModule statsModule = actor.GetModule<StatsModule>();
            _addedModifier = new StatModifier(data);
            statsModule.AddModifier(_addedModifier);
            // float scale = 1;
            // if (RarityScales != null && RarityScales.Length > 0)
            // {
            //     scale = RarityScales[rarity % RarityScales.Length];
            // }
        }

        public override void RemoveAffixFromActor(Actor actor)
        {
            if(_addedModifier == null) return;
            StatsModule statsModule = actor.GetModule<StatsModule>();
            statsModule.RemoveModifier(_addedModifier);
        }

        public StatModifierData GetStatModifierData()
        {
          
            StatModifierData statModifierData = new StatModifierData();
            AttributeAsset attributeAsset = RpgManager.GetAttributeAssetById(AttributeId);
            statModifierData.Stat = attributeAsset;
            return statModifierData;
        }
    }
}