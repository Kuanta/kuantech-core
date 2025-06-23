using Kuantech.Core.FX;
using Kuantech.Core.HyperCasual;
using Kuantech.HyperCasual.UI;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Midcore.UI
{
    public class UnitPurchaseButton : Draggable
    {
        [Header("Unit To Spawn")] 
        [SerializeField] private ActorBlueprint UnitToSpawn;

        [Header("Components")] 
        [SerializeField] private Image Icon;

        [Header("Price Tag")] 
        [SerializeField] private PriceTag PriceTag;

        [Header("Effects")] [KTTag("AudioTags")]
        public int PurchaseSfx = 0;
        
        public void SetUnitToSpawn(ActorBlueprint blueprint)
        {
            UnitToSpawn = blueprint;
            if (Icon != null)
            {
                Icon.sprite = blueprint.ProgressableDataAsset.Icon;
            }
        }
        
        public override bool CanBeDragged()
        {
            if (UnitToSpawn == null || !base.CanBeDragged()) return false;
            return CanAfford();
        }

        public override bool CanBeDropped()
        {
            if(!CanAfford()) return false;
            return CanAfford(); //A final check before dropping
        }

        public override void OnSuccesfullDrop()
        {
            base.OnSuccesfullDrop();
            
            //Pay the price
            BuyUnit();
            AudioLibrary.PlaySoundByTag(PurchaseSfx);
        }
        
        public override GameObject GetGhostInstance()
        {
            if (UnitToSpawn == null) return null;
            return UnitToSpawn.CreateActor()?.gameObject;
        }
        
        #region Transactions
        public bool CanAfford()
        {
            StoreManager sm = StoreManager.GetContext<StoreManager>();
            if (sm == null) return true; //todo: Maybe we can 'not' use store manager
            return sm.CanBeBought(UnitToSpawn.BuyableInfo);
        }

        public void BuyUnit()
        {
            StoreManager sm = StoreManager.GetContext<StoreManager>();
            if (sm == null) return; //todo: Maybe we can 'not' use store manager
            sm.BuyItem(UnitToSpawn.BuyableInfo);
        }
        #endregion
    }
}