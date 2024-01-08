using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    public class VendorUnlocker : Unlocker
    {
        [SerializeField] private GoodsShelf GoodsShelf;

        public override void PostInitialize()
        {
            base.PostInitialize();
            if(GetProgress() >= 1.0f)
            {
                GoodsShelf.SpawnVendor();
            }
        }
        public override void OnFilled()
        {
            base.OnFilled();
            GoodsShelf.SpawnVendor();
        }
    }
}