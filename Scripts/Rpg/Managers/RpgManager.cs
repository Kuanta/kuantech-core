using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kuantech.Core;
using Kuantech.Core.Combat;
using Kuantech.Rpg.Skills;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Rpg.Managers
{
    public class RpgManager : SubManager
    {
        [Header("Stats")]
        public List<AttributeAsset> AttributeAssets;
        private Dictionary<string, AttributeAsset> _attributesById;

        public List<ResourceAsset> ResourceAssets;
        private Dictionary<string, ResourceAsset> _resourcesById;

        public List<DamageType> DamageTypes;
        private Dictionary<string, DamageType> _damageTypesById;
        
        [Header("Skills")]
        public List<SkillDataAsset> SkillDataAssets = new List<SkillDataAsset>();

        [Header("Status Effects")]
        public List<StatusEffectAsset> StatusEffectAssets = new List<StatusEffectAsset>();
        private Dictionary<string, StatusEffectAsset> _statusEffectsById;

        private Dictionary<string, SkillDataAsset> _skillsById;
        
        public override async UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);
            _skillsById = new Dictionary<string, SkillDataAsset>();
            if (SkillDataAssets != null)
            {
                foreach (var dataAsset in SkillDataAssets)
                {
                    if(dataAsset.SkillId.IsNullOrEmpty()) continue;
                    _skillsById[dataAsset.SkillId] = dataAsset;
                }                
            }

            //Atts
            if(AttributeAssets != null)
            {
                _attributesById = new Dictionary<string, AttributeAsset>();
                foreach (var attributeAsset in AttributeAssets)
                {
                    _attributesById[attributeAsset.GetId()] = attributeAsset;
                }
            }

            //Resources
            if(ResourceAssets != null)
            {
                _resourcesById = new Dictionary<string, ResourceAsset>();
                foreach (var resourceAsset in ResourceAssets)
                {
                    _resourcesById[resourceAsset.Id] = resourceAsset;
                }
            }

            //Damage Types
            if (DamageTypes != null)
            {
                _damageTypesById = new Dictionary<string, DamageType>();
                foreach (var damageType in DamageTypes)
                {
                    _damageTypesById[damageType.GetId()] = damageType;
                }
            }

            if(StatusEffectAssets != null)
            {
                _statusEffectsById = new Dictionary<string, StatusEffectAsset>();
                foreach (var statusEffectAsset in StatusEffectAssets)
                {
                    _statusEffectsById[statusEffectAsset.GetId()] = statusEffectAsset;
                }
            }
        }

        #region Attributes
        public static AttributeAsset GetAttributeAssetById(string id)
        {
            var ctx = GetContext<RpgManager>();
            if (ctx == null || id==null) return null;
            if(ctx._attributesById.ContainsKey(id))
            {
                return ctx._attributesById[id];
            }
            return null;
        }

        public static ResourceAsset GetResourceAssetById(string id)
        {
            var ctx = GetContext<RpgManager>();
            if (ctx == null) return null;
            if (ctx._resourcesById.ContainsKey(id))
                return ctx._resourcesById[id];
            return null;
        }

        public static DamageType GetDamageTypeById(string id)
        {
            var ctx = GetContext<RpgManager>();
            if (ctx == null) return null;
            if (ctx._damageTypesById != null && ctx._damageTypesById.ContainsKey(id))
                return ctx._damageTypesById[id];
            return null;
        }
        #endregion

        #region Skills
        public static SkillDataAsset GetSkillDataAssetById(string id)
        {
            var ctx = GetContext<RpgManager>();
            if (ctx == null) return null;
            if (ctx._skillsById.ContainsKey(id))
            {
                return ctx._skillsById[id];
            }

            return null;
        }
        #endregion

        #region Status Effects
        public static StatusEffectAsset GetStatusEffectAssetById(string id)
        {
            var ctx = GetContext<RpgManager>();
            if (ctx == null) return null;
            if(ctx._statusEffectsById.ContainsKey(id))
            {
                return ctx._statusEffectsById[id];
            }
            return null;
        }
        #endregion
    }
}