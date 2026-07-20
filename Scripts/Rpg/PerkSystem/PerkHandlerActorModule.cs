using Kuantech.Core;

namespace Kuantech.Rpg
{
    public class PerkHandlerActorModule : ActorModule
    {
        private PerkHandler _perkHandler;

        public override void Initialize()
        {
            base.Initialize();
            _perkHandler = new PerkHandler();
        }
        
        /// <summary>
        /// Adds a perk
        /// </summary>
        /// <param name="perkAsset"></param>
        public void AddPerk(PerkAsset perkAsset)
        {
            Perk perk = _perkHandler.AddPerk(perkAsset);
            if (perk == null) return; // already owned → ranked up (already re-applied), or creation failed
            perk.Bind(Actor);
            perk.Apply();
        }
        
        public void IncreasePerkRank(PerkAsset perkAsset)
        {
            if (!_perkHandler.IsPerkUnlocked(perkAsset))
            {
                AddPerk(perkAsset);
                return;
            }
            _perkHandler.RankUpPerk(perkAsset);
        }
        
        /// <summary>
        /// Gets perk
        /// </summary>
        /// <param name="perkAsset"></param>
        /// <returns></returns>
        public Perk GetPerk(PerkAsset perkAsset)
        {
            if (_perkHandler == null) return null;
            return _perkHandler.GetPerk(perkAsset);
        }

        public int GetCurrentPerkRank(PerkAsset perkAsset)
        {
            if (_perkHandler == null) return -1;
            return _perkHandler.GetCurrentPerkRank(perkAsset);
        }

        public override void ResetModule()
        {
            _perkHandler.ClearPerks();
        }
    }
}