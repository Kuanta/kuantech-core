using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Midcore
{
    public class DeckBuildingManager : SubManager
    {
        [Header("Default Deck")]
        public List<Deck> DefaultDecks;

        [HideInInspector] [SaveableField] public List<Deck> Decks;

        public override async UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);
            foreach (var deck in Decks)
            {
                deck.Initialize();
            }
        }

        public override void OnSubmanagersInitialized()
        {
            base.OnSubmanagersInitialized();

            foreach (var deck in Decks)
            {
                deck.SetEquippedCollectibles();
            }
        }

        public override void LoadState()
        {
            base.LoadState();
            if (Decks.IsNullOrEmpty()) return;
            foreach (var deck in Decks)
            {
                deck.CheckDeckIntegrity();
            }    
        }
        
        public override void SetDefaultState()
        {
            base.SetDefaultState();
            Decks = new List<Deck>();
            foreach (var deck in DefaultDecks)
            {
                Deck newDeck = new Deck();
                newDeck.CurrentDeck = new List<ProgressibleData>(deck.CurrentDeck);
                newDeck.DeckSize = deck.DeckSize;
                newDeck.DefaultDeck = deck.DefaultDeck;
                newDeck.Initialize();
                newDeck.SetDefaultState();
                Decks.Add(newDeck);
            }
        }

        public Deck GetDeck(int deckId)
        {
            foreach (var deck in Decks)
            {
                if (deck.DeckIndex == deckId) return deck;
            }

            return null;
        }
        
        public static int GetDeckSize(int deckId)
        {
            var ctx = DeckBuildingManager.GetContext<DeckBuildingManager>();
            if (ctx == null) return 0;
            Deck deck = ctx.GetDeck(deckId);
            if (deck == null) return 0;
            return deck.DeckSize;
        }
        
        
        public static List<ProgressibleData> GetCurrentDeck(int deckId)
        {
            var ctx = DeckBuildingManager.GetContext<DeckBuildingManager>();
            if (ctx == null) return null;
            Deck deck = ctx.GetDeck(deckId);
            if (deck == null) return null;
            return deck.CurrentDeck;
        }
        
        /// <summary>
        /// Returns a list of currently equipped collectable assets
        /// </summary>
        /// <returns></returns>
        public static List<CollectableAsset> GetCurrentCollectableAssets(int deckIndex)
        {
            var ctx = DeckBuildingManager.GetContext<DeckBuildingManager>();
            if (ctx == null) return null;
            Deck deck = ctx.GetDeck(deckIndex);
            if (deck == null) return null;
            return deck.GetCurrentCollectableAssets();
        }
        
        public static bool IsEquipped(CollectableAsset asset)
        {
            var ctx = GetContext<DeckBuildingManager>();
            if (ctx == null || asset == null) return false;
            Deck deck = ctx.GetDeck(asset.DeckIndex);
            if (deck == null) return false;
            return deck.IsEquipped(asset);
        }
        
        /// <summary>
        /// Equips collectable to a suitable slot in the current deck.
        /// </summary>
        /// <param name="collectible"></param>
        public static bool EquipCollectible(CollectableAsset collectible)
        {
            if (IsEquipped(collectible)) return false;
            var ctx = DeckBuildingManager.GetContext<DeckBuildingManager>();
            if (ctx == null || collectible == null) return false;

            Deck deck = ctx.GetDeck(collectible.DeckIndex);
            if (deck == null) return false;
            if (deck.EquipCollectible(collectible))
            {
                ctx.SaveState();
                return true;
            }

            return false;
        }
        
        /// <summary>
        /// Unequips a collectible
        /// </summary>
        /// <param name="collectible"></param>
        public static bool UnequipCollectible(CollectableAsset collectible)
        {
            var ctx = DeckBuildingManager.GetContext<DeckBuildingManager>();
            if (ctx == null || collectible == null) return false;
            Deck deck = ctx.GetDeck(collectible.DeckIndex);
            if (deck == null) return false;

            if (deck.UnequipCollectible(collectible))
            {
                ctx.SaveState();
                return true;
            }

            return false;
        }

        public static void ReplaceCollectible(CollectableAsset oldAsset, CollectableAsset newAsset)
        {
            if (UnequipCollectible(oldAsset))
            {
                EquipCollectible(newAsset);
            }
        }
    }
}