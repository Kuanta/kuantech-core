using System.Collections.Generic;

namespace Kuantech.Rpg
{
    /// <summary>
    /// A generic perk handler class. 
    /// </summary>
    public class PerkHandler
    {
        //Perk container
        public Dictionary<PerkAsset, PerkData> PerkDatas;
        
        public bool IsPerkUnlocked(PerkAsset perkAsset)
        {
            if (PerkDatas == null) return false;
            return PerkDatas.ContainsKey(perkAsset);
        }
        
        public PerkData GetPerkData(PerkAsset perkAsset)
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
            return GetPerkData(perkAsset).CurrentRank;
        }
        
        /// <summary>
        /// Adds a perk if its not already unlocked. If its unlocked, increases the rank
        /// </summary>
        /// <param name="perkAsset"></param>
        public void AddPerk(PerkAsset perkAsset)
        {
            if (IsPerkUnlocked(perkAsset))
            {
                RankUpPerk(perkAsset);
                return;
            }

            PerkData perkData = new PerkData()
            {
                PerkAsset = perkAsset,
                CurrentRank = 0,
            };
            if (PerkDatas == null)
            {
                PerkDatas = new Dictionary<PerkAsset, PerkData>();
            }
            PerkDatas.Add(perkAsset, perkData);
            perkData.Apply();
        }

        public void RankUpPerk(PerkAsset perkAsset)
        {
            PerkData perkData = GetPerkData(perkAsset);
            if (perkData == null)
            {
                AddPerk(perkAsset);
                return;
            }
            perkData.IncreaseRank();
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