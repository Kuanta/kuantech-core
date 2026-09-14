using System;
using Kuantech.Rpg;

namespace Kuantech.RogueLike
{
    [Serializable]
    public struct PerkSelectionData
    {
        public PerkAsset PerkAsset;
        public float PerkAppearChance;
        public int PerkRank;
    }
}