using UnityEngine;

namespace Kuantech.Core.HyperCasual.Runner
{
    public class CurrencyPickup : Pickupable
    {
        public Currencies Currency;
        public int Amount = 1;
        
        protected override void OnPickup(Collider other)
        {
            base.OnPickup(other);
            ((HCGameManager)HCGameManager.Instance).CurrentLevel.AddCurrency((int)Currency, Amount);
        }
    }
}