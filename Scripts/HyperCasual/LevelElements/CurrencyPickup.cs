using UnityEngine;

namespace Kuantech.Core.HyperCasual.Runner
{
    public class CurrencyPickup : Pickupable
    {
        public string CurrencyId;
        public int Amount = 1;
        
        protected override void OnPickup(Collider other)
        {
            base.OnPickup(other);

            LevelManager.GetContext<LevelManager>().CurrentLevel.AddCurrency(CurrencyId, Amount);
        }
    }
}