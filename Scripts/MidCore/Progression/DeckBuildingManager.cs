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
        
        [Header("Default Deck")]
        public List<DeckCollectableAsset> DefaultDeck;

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

        public override void SetDefaultState()
        {
            base.SetDefaultState();
            CurrentDeck.Clear();
            for (int i = 0; i < DeckSize; ++i)
            {
                CurrentDeck.Add(null);
            }
            for (int i = 0; i < DefaultDeck.Count; ++i)
            {
                if(i>= DeckSize) break;
                if(DefaultDeck[i] == null) continue;
                EquipCollectible(DefaultDeck[i]);
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
            if (!ctx._collectiblesById.ContainsKey(id)) return null;
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

        public static DeckCollectableAsset GetCollectible(string collectibleId)
        {
            var ctx = DeckBuildingManager.GetContext<DeckBuildingManager>();
            if (ctx == null || collectibleId == null) return null;
            if (ctx._collectiblesById.ContainsKey(collectibleId)) return ctx._collectiblesById[collectibleId];
            return null;
        }
        
        public static List<ProgressibleData> GetCurrentDeck()
        {
            var ctx = DeckBuildingManager.GetContext<DeckBuildingManager>();
            if (ctx == null) return null;
            return ctx.CurrentDeck;
        }
        
        /// <summary>
        /// Returns a list of currently equipped collectable assets
        /// </summary>
        /// <returns></returns>
        public static List<DeckCollectableAsset> GetCurrentCollectableAssets()
        {
            var ctx = DeckBuildingManager.GetContext<DeckBuildingManager>();
            if (ctx == null) return null;
            List<DeckCollectableAsset> equippedAssets = new List<DeckCollectableAsset>();
            foreach (var data in ctx.CurrentDeck)
            {
                if(data == null || data.Id.IsNullOrEmpty()) continue;
                DeckCollectableAsset asset = GetCollectible(data.Id);
                if(asset != null) equippedAssets.Add(asset);
            }
            return equippedAssets;
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

                if (ctx.CurrentDeck[i] == null || ctx.CurrentDeck[i].Id.IsNullOrEmpty())
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