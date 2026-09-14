using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Rpg;
using Kuantech.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Kuantech.RogueLike
{
    [System.Serializable]
    public class PerkSelectionHandler
    {
        public List<PerkAsset> CommonPerks;
        [NonSerialized] public List<PerkAsset> AvailablePerks;

        private PerkHandlerActorModule _perkHandler;

        public void Initialize()
        {
            AvailablePerks = new List<PerkAsset>();
            AvailablePerks.AddRange(CommonPerks);
        }

        public void AddToAvailablePerks(List<PerkAsset> perkAssets)
        {
            foreach(var asset in perkAssets)
            {
                AddAvailablePerk(asset);
            }
        }
        public void AddAvailablePerk(PerkAsset perkAsset)
        {
            AvailablePerks.Add(perkAsset);
        }

        public void AttachActor(Actor actor)
        {
            _perkHandler = actor.GetModule<PerkHandlerActorModule>();
        }

        public void SelectPerk(PerkSelectionData perkData)
        {
            if (perkData.PerkAsset == null || _perkHandler == null) return;
            _perkHandler.AddPerk(perkData.PerkAsset);
        }

        /// <summary>
        /// Draws up to <paramref name="numberOfPerks"/> DISTINCT perks, weighted by each asset's own
        /// PerkAppearChance, excluding perks the player has already maxed out. May return fewer if the
        /// eligible pool is small. PerkRank is computed here, per draw — it's not stored anywhere; it's
        /// just "the rank picking this card right now would give you".
        /// </summary>
        public List<PerkSelectionData> DrawPerkSelection(int numberOfPerks)
        {
            var result = new List<PerkSelectionData>();
            if (AvailablePerks.IsNullOrEmpty()) return result;

            // Eligible pool: not maxed. Kept as parallel lists so we can draw without replacement (remove
            // picked entries) — no shared WeightedProbabilityArray side effects.
            var pool = new List<PerkAsset>();
            var weights = new List<float>();
            foreach (var perkAsset in AvailablePerks)
            {
                if (perkAsset == null) continue;
                if (IsMaxed(perkAsset)) continue;
                if (!DependencyMet(perkAsset)) continue;

                pool.Add(perkAsset);
                weights.Add(Mathf.Max(0f, perkAsset.PerkAppearChance));
            }

            int count = Mathf.Min(numberOfPerks, pool.Count);
            for (int i = 0; i < count; i++)
            {
                int idx = WeightedPick(weights);
                if (idx < 0) break;

                PerkAsset asset = pool[idx];
                int existingRank = _perkHandler != null ? _perkHandler.GetCurrentPerkRank(asset) : -1;
                result.Add(new PerkSelectionData
                {
                    PerkAsset = asset,
                    PerkAppearChance = asset.PerkAppearChance,
                    PerkRank = existingRank + 1, // -1 (not owned) → 0 first rank; owned → next rank
                });

                pool.RemoveAt(idx);
                weights.RemoveAt(idx);
            }
            return result;
        }

        private bool IsMaxed(PerkAsset asset)
        {
            if (_perkHandler == null) return false;
            Perk perk = _perkHandler.GetPerk(asset);
            if (perk == null) return false; // not owned yet → eligible
            return perk.CurrentRank >= asset.MaxRank;
        }

        // A perk with a DependentPerk set only enters the pool once the actor already owns that other
        // perk — e.g. an upgraded modifier that shouldn't show up before its base perk was picked.
        private bool DependencyMet(PerkAsset asset)
        {
            if (asset.DependentPerk == null) return true;
            if (_perkHandler == null) return false; // can't verify ownership without an attached actor
            return _perkHandler.GetCurrentPerkRank(asset.DependentPerk) >= 0;
        }

        // Weighted index pick from a parallel weights list. Returns -1 if empty.
        private static int WeightedPick(List<float> weights)
        {
            if (weights.Count == 0) return -1;

            float total = 0f;
            foreach (var w in weights) total += w;
            if (total <= 0f) return 0; // all zero weight → just take the first

            float roll = Random.Range(0f, total);
            float acc = 0f;
            for (int i = 0; i < weights.Count; i++)
            {
                acc += weights[i];
                if (roll <= acc) return i;
            }
            return weights.Count - 1;
        }
    }
}
