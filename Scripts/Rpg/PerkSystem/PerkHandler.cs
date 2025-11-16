using System.Collections.Generic;

namespace Kuantech.Rpg
{
    /// <summary>
    /// A generic perk handler class. 
    /// </summary>
    public class PerkHandler
    {
        //Perk container
        public Dictionary<PerkAsset, Perk> PerkDatas;
        
        public bool IsPerkUnlocked(PerkAsset perkAsset)
        {
            if (PerkDatas == null) return false;
            return PerkDatas.ContainsKey(perkAsset);
        }
        
        public Perk GetPerk(PerkAsset perkAsset)
        {
            if (PerkDatas != null && PerkDatas.ContainsKey(perkAsset))
            {
                return PerkDatas[perkAsset];
            }
            return null;
        }
        
        /// <summary>
        /// Returns the current rank of a perk
        /// </summary>
        /// <param name="perkAsset"></param>
        /// <returns></returns>
        public int GetCurrentPerkRank(PerkAsset perkAsset)
        {
            if (!IsPerkUnlocked(perkAsset)) return -1;
            return GetPerk(perkAsset).CurrentRank;
        }
        
        /// <summary>
        /// Adds a perk if its not already unlocked. If its unlocked, increases the rank
        /// </summary>
        /// <param name="perkAsset"></param>
        public Perk AddPerk(PerkAsset perkAsset)
        {
            if (IsPerkUnlocked(perkAsset))
            {
                RankUpPerk(perkAsset);
                return null;
            }

            Perk perk = new Perk()
            {
                PerkAsset = perkAsset,
                CurrentRank = 0,
            };
            if (PerkDatas == null)
            {
                PerkDatas = new Dictionary<PerkAsset, Perk>();
            }
            PerkDatas.Add(perkAsset, perk);
            perk.UpdatePerkEffect();
            return perk;
        }

        public void RankUpPerk(PerkAsset perkAsset)
        {
            Perk perk = GetPerk(perkAsset);
            if (perk == null)
            {
                AddPerk(perkAsset);
                return;
            }
            perk.IncreaseRank();
        }
        
        /// <summary>
        /// Removes all perks
        /// </summary>
        public void ClearPerks()
        {
            if (PerkDatas == null) return;
            PerkDatas.Clear();
        }
        
        
    }
}