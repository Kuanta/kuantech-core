using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kuantech.Core;
using Kuantech.Rpg.Skills;
using Kuantech.Utils;

namespace Kuantech.Rpg.Managers
{
    public class RpgManager : SubManager
    {
        public List<SkillDataAsset> SkillDataAssets = new List<SkillDataAsset>()
            ;
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
        }

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
    }
}