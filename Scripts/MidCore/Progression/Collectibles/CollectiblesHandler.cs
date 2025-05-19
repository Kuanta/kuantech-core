using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.HyperCasual;
using Kuantech.Rpg;
using Kuantech.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.Midcore
{
    [Serializable]
    public class CollectibleData
    {
        public string CollectibleId;
        public LevelVariable Level;
    }
    
    public class CollectiblesHandler: ISaveable
    {
        [Serializable]
        private class Wrapper
        {
            public List<string> ids;
        }
        
        [Header("Collectibles")]
        public List<CollectibleDataAsset> AllCollectibles;
        [SaveableField] private Dictionary<string, CollectibleData> _collectibleDatas;
        private Dictionary<string, CollectibleDataAsset> _collectiblesById;

        #region Queries
        public bool IsCollectibleUnlocked(CollectibleDataAsset dataAsset)
        {
            return GetCollectibleData(dataAsset) != null;
        }
        
        [Button("Unlock Collectible")]
        public bool UnlockCollectible(CollectibleDataAsset dataAsset)
        {
            if (IsCollectibleUnlocked(dataAsset)) return false; //Already unlocked
            
            if (!CanAffordCollectible(dataAsset)) return false;

            if (IsCollectibleUnlocked(dataAsset)) return true;
            
            //Add entry
            _collectibleDatas[dataAsset.CollectibleId] = CreateDataEntry(dataAsset);
            
            //Buy if needed
            StoreManager sm = StoreManager.GetContext<StoreManager>();
            sm.BuyItem(dataAsset.StoreEntryId, 0, 0); //Collectibles have no rank
            return true;
        }

        public CollectibleData GetCollectibleData(CollectibleDataAsset dataAsset)
        {
            if(_collectibleDatas.IsNullOrEmpty()) return null;
            if (_collectibleDatas.ContainsKey(dataAsset.CollectibleId))
                return _collectibleDatas[dataAsset.CollectibleId];
            return null;
        }
        private CollectibleData CreateDataEntry(CollectibleDataAsset dataAsset)
        {
            return new CollectibleData()
            {
                Level = new LevelVariable(),
                CollectibleId = dataAsset.CollectibleId,
            };
        }
        public bool CanAffordCollectible(CollectibleDataAsset dataAsset)
        {
            if (dataAsset.StoreEntryId.IsNullOrEmpty()) return true;
            StoreManager sm = StoreManager.GetContext<StoreManager>();
            if (!sm.CanBeBought(dataAsset.StoreEntryId, 0, 0)) return false;
            return true;
        }

        public void AddExperienceToCollectible(CollectibleDataAsset dataAsset, int amount)
        {
            if (dataAsset == null) return;
            if (_collectibleDatas.IsNullOrEmpty()) return;
            if (!_collectibleDatas.ContainsKey(dataAsset.CollectibleId)) return;
            _collectibleDatas[dataAsset.CollectibleId].Level.AddValue(amount);
        }

        public void SetCollectibleLevel(CollectibleDataAsset dataAsset, int level)
        {
            if (dataAsset == null) return;
            CollectibleData data = GetCollectibleData(dataAsset);
            if (data == null) return;
            data.Level.SetLevel(level);
        }
        
        public void LevelupCollectible(CollectibleDataAsset dataAsset)
        {
            if (dataAsset == null) return;
            CollectibleData data = GetCollectibleData(dataAsset);
            if (data == null) return;
            data.Level.LevelUp();
        }
        #endregion
        
        #region Save Load
        public byte[] Serialize()
        {
            return null;
        }

        public void Deserialize(byte[] data)
        {
           
        }
        #endregion
      
    }
}