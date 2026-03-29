using System.Collections.Generic;
using ArcadeIdle.Data;
using Cysharp.Threading.Tasks;
using Kuantech.Core;
using Kuantech.Core.Store;
using Kuantech.Inventory;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    public class ArcadeIdleManager : SubManager
    {
        [Header("Npc")]
        [SerializeField] private List<ArcadeIdleNpc> NpcList;
        private Dictionary<int, List<ArcadeIdleNpc>> _tagsToNpcList;
        public int NpcHirePrice = 200; //Very temporary
        public CurrencyAsset NpcHireCurrency;

        [Header("Resources")]
        [SerializeField] private ResourceShop ResourceShop;

        public List<ResourceData> Resources;
        public ResourcesVault ResourcesVault;
        //private Dictionary<string, ResourceData> _idToResourceData;


        //todo: These should be spawned
        public ArcadeIdlePlayer Player;
        public ArcadeIdleVenue CurrentVenue;

        public override async UniTask Initialize(GameManager parentManager)
        {
            await base.Initialize(parentManager);

            //Npcs
            _tagsToNpcList = new Dictionary<int, List<ArcadeIdleNpc>>();
            foreach(var npcPrefab in NpcList)
            {
                if(!_tagsToNpcList.ContainsKey(npcPrefab.CharacterTag) || _tagsToNpcList[npcPrefab.CharacterTag] == null)
                {
                    _tagsToNpcList[npcPrefab.CharacterTag] = new List<ArcadeIdleNpc>();
                }
                _tagsToNpcList[npcPrefab.CharacterTag].Add(npcPrefab);
            }
            //Resources
            if (ResourcesVault != null)
            {
                await ResourcesVault.LoadDataFromList(Resources);
            }
            
            //todo: Read resource datas from db
            
            // foreach(var resource in ResourcesList)
            // {
            //     _idToResourceData[resource.ResourceId] = resource;
            // }

            if (ResourceShop != null)
            {
                ResourceShop.OnOrderArrivedEvent += OnResourcesArrived;
            }
        }

        public override void OnSubmanagersInitialized()
        {
            base.OnSubmanagersInitialized();
            Player.Initialize(); //todo: Instantiate player in the future
            Player.PostInitialize();
            CurrentVenue.Initialize(); //todo: This is temporary

            //Initialize current venue

        }

        #region Player
        public ArcadeIdlePlayer GetPlayer()
        {
            return Player;
        }
        #endregion
        
        #region Npcs
        public static ArcadeIdleNpc GetRandomNpcByTag(int tag)
        {
            ArcadeIdleManager context = GetContext<ArcadeIdleManager>();
            if (context == null) return null;
            return context._tagsToNpcList[tag].GetRandomElement();
        }

        public static bool HireWorker(int tag)
        {
            ArcadeIdleManager context = GetContext<ArcadeIdleManager>();
            if (context == null || context.CurrentVenue == null) return false;
            //CurrencyModel cm = GameStateManager.GetModuleStatic<CurrencyModel>();
            int heldAmount = 0; //todo(currency): Fix here
            int requiredAmount = context.NpcHirePrice;

            if(requiredAmount > heldAmount) return false;
            //m.RemoveCurrency(context.NpcHireCurrency.CurrencyId, requiredAmount);
            context.CurrentVenue.HireWorker(GetRandomNpcByTag(tag));
            return true;
        } 

        public static int GetWorkerHirePrice(int workerTag)
        {
            ArcadeIdleManager context = GetContext<ArcadeIdleManager>();
            if (context == null || context.CurrentVenue == null) return 0;

            return context.NpcHirePrice;
        }
        #endregion

        #region Resource Access
        public static ResourceData GetResourceData(string id)
        {
            ArcadeIdleManager context = GetContext<ArcadeIdleManager>();
            if(context == null) return null;
            ItemData itemData = context.ResourcesVault.GetDataById(id);
            return itemData as ResourceData;
        }
        #endregion

        #region Resource Shop
        public ResourceShop GetResourceShop()
        {
            return ResourceShop;
        }

        private void OnResourcesArrived(object sender, Dictionary<ResourceData, int> orderedResources)
        {
            if(CurrentVenue == null)
            {
                Debug.LogError("Current venue is null!");
                return;
            }
            CurrentVenue.OnOrderArrived(orderedResources);
        }
        #endregion

    }
}