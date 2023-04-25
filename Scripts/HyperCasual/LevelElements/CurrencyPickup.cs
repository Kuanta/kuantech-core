namespace Kuantech.Core.HyperCasual
{
    public class CurrencyPickup : Pickupable
    {
        public Currencies Currency;
        public int Amount = 1;
        
        protected override void OnPickup()
        {
            base.OnPickup();
            ((HCGameManager)HCGameManager.Instance).CurrentLevel.AddCurrency((int)Currency, Amount);
        }
    }
}