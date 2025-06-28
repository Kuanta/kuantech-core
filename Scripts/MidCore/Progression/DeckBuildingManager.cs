using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Midcore
{
    public class DeckBuildingManager : SubManager
    {
        [Header("Properties")] 
        public int DeckSize = 4;
        
        [Header("Deck Collectibles")]
        public List<DeckCollectableAsset> DeckCollectibles;
        
        //Current Deck
        [SaveableField] public List<ProgressibleData> CurrentDeck;
        
        private Dictionary<string, DeckCollectableAsset> _collectiblesById = new Dictionary<string, DeckCollectableAsset>();
        private Dictionary<string, DeckCollectableAsset> _equippedCollectiblesById = new Dictionary<string, DeckCollectableAsset>();

        public override async UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);
            for (int i = 0; i < DeckSize; ++i)
            {
                CurrentDeck.Add(null);
            }

            foreach (var collectible in DeckCollectibles)
            {
                _collectiblesById[collectible.GetId()] = collectible;
            }
        }

        public static int GetDeckSize()
        {
            var ctx = DeckBuildingManager.GetContext<DeckBuildingManager>();
            if (ctx == null) return 0;
            return ctx.DeckSize;
        }
        
        /// <summary>
        /// Returns a collectible by its ID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static DeckCollectableAsset GetProgressibleDataAssetById(string id)
        {
            var ctx = DeckBuildingManager.GetContext<DeckBuildingManager>();
            if (ctx == null) return null;
            if (string.IsNullOrEmpty(id)) return null;
            if (ctx._collectiblesById.ContainsKey(id)) return null;
            return ctx._collectiblesById[id];
        }
        
        /// <summary>
        /// Returns all collectibles
        /// </summary>
        /// <returns></returns>
        public static List<DeckCollectableAsset> GetCollectibles()
        {
            var ctx = DeckBuildingManager.GetContext<DeckBuildingManager>();
            if (ctx == null) return null;
            return ctx.DeckCollectibles;
        }
        
        public static List<ProgressibleData> GetCurrentDeck()
        {
            var ctx = DeckBuildingManager.GetContext<DeckBuildingManager>();
            if (ctx == null) return null;
            return ctx.CurrentDeck;
        }

        public static bool IsEquipped(DeckCollectableAsset asset)
        {
            var ctx = GetContext<DeckBuildingManager>();
            if (ctx == null || asset == null) return false;
            return ctx._equippedCollectiblesById.ContainsKey(asset.GetId());
        }
        
        /// <summary>
        /// Equips collectable to a suitable slot in the current deck.
        /// </summary>
        /// <param name="collectible"></param>
        public static bool EquipCollectible(DeckCollectableAsset collectible)
        {
            var ctx = DeckBuildingManager.GetContext<DeckBuildingManager>();
            if (ctx == null || collectible == null) return false;
            
            //Is there empty slot
            for (int i = 0; i < ctx.DeckSize; ++i)
            {
                if (!ctx.CurrentDeck.IsValidIndex(i))
                {
                    ctx.CurrentDeck.Add(new ProgressibleData(collectible));
                    ctx._equippedCollectiblesById[collectible.GetId()] = collectible;
                    ctx.SaveState();
                    return true;
                }

                if (ctx.CurrentDeck[i] == null)
                {
                    ctx.CurrentDeck[i] = new ProgressibleData(collectible);
                    ctx._equippedCollectiblesById[collectible.GetId()] = collectible;
                    ctx.SaveState();
                    return true;
                }
            }
            //Unequip a collectible first
            return false;
        }
        
        /// <summary>
        /// Unequips a collectible
        /// </summary>
        /// <param name="collectible"></param>
        public static bool UnequipCollectible(DeckCollectableAsset collectible)
        {
            var ctx = DeckBuildingManager.GetContext<DeckBuildingManager>();
            if (ctx == null) return false;
            if (!ctx._equippedCollectiblesById.ContainsKey(collectible.GetId()))
            {
                return false;
            }
            for(int i=0;i<ctx.CurrentDeck.Count; ++i)
            {
                if (ctx.CurrentDeck.IsValidIndex(i) && ctx.CurrentDeck[i] != null && ctx.CurrentDeck[i].Id== collectible.GetId())
                {
                    ctx.CurrentDeck[i] = null;
                    ctx._equippedCollectiblesById.Remove(collectible.GetId());
                    ctx.SaveState();
                    return true;
                }
            }

            return false;
        }

        public static void ReplaceCollectible(DeckCollectableAsset oldAsset, DeckCollectableAsset newAsset)
        {
            if (UnequipCollectible(oldAsset))
            {
                EquipCollectible(newAsset);
            }
        }
    }
}