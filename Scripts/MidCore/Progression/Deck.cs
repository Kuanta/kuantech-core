using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Midcore
{
    [Serializable]
    public class Deck
    {
        public int DeckIndex;
        public int DeckSize = 4;
        
        [Header("Default Deck")]
        public List<CollectableAsset> DefaultDeck;
        
        [SaveableField] public List<ProgressibleData> CurrentDeck;
        private Dictionary<string, CollectableAsset> _equippedCollectiblesById = new Dictionary<string, CollectableAsset>();

        public void Initialize()
        {
            for (int i = 0; i < DeckSize; ++i)
            {
                CurrentDeck.Add(null);
            }
        }

        public void SetEquippedCollectibles()
        {
            foreach (var equipped in CurrentDeck)
            {
                if(equipped == null || equipped.Id.IsNullOrEmpty()) continue;
                _equippedCollectiblesById[equipped.Id] = ProgressionManager.GetCollectibleById(equipped.Id);
            }
        }

        public void SetDefaultState()
        {
            CurrentDeck.Clear();
            _equippedCollectiblesById = new Dictionary<string, CollectableAsset>();
            
            
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

        public List<CollectableAsset> GetCurrentCollectableAssets()
        {
            List<CollectableAsset> equippedAssets = new List<CollectableAsset>();
            foreach (var data in CurrentDeck)
            {
                if(data == null || data.Id.IsNullOrEmpty()) continue;
                CollectableAsset asset = ProgressionManager.GetCollectibleById(data.Id);
                if(asset != null) equippedAssets.Add(asset);
            }
            return equippedAssets;
        }
        
        public bool EquipCollectible(CollectableAsset collectible)
        {
            //Is there empty slot
            for (int i = 0; i < DeckSize; ++i)
            {
                if (!CurrentDeck.IsValidIndex(i))
                {
                    CurrentDeck.Add(new ProgressibleData(collectible));
                    _equippedCollectiblesById[collectible.GetId()] = collectible;
                    return true;
                }

                if (CurrentDeck[i] == null || CurrentDeck[i].Id.IsNullOrEmpty())
                {
                    CurrentDeck[i] = new ProgressibleData(collectible);
                    _equippedCollectiblesById[collectible.GetId()] = collectible;
                    return true;
                }
            }
            
            //Unequip a collectible first
            return false;
        }

        public bool UnequipCollectible(CollectableAsset collectible)
        {
            if (!_equippedCollectiblesById.ContainsKey(collectible.GetId()))
            {
                return false;
            }
            for(int i=0;i<CurrentDeck.Count; ++i)
            {
                if (CurrentDeck.IsValidIndex(i) && CurrentDeck[i] != null && CurrentDeck[i].Id== collectible.GetId())
                {
                    CurrentDeck[i] = null;
                    _equippedCollectiblesById.Remove(collectible.GetId());
                    return true;
                }
            }

            return false;
        }

        public bool IsEquipped(CollectableAsset collectible)
        {
            return _equippedCollectiblesById.ContainsKey(collectible.GetId());
        }
    }
}