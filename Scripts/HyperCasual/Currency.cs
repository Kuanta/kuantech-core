using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public enum Currencies
    {
        Coin = 0,
        Gem = 1,
    }
    public struct Currency
    {
        public int Amount;
        public int CurrencyId;

        public void SetAmount(int amount)
        {
            Amount = amount;
            Amount = Mathf.Max(Amount, 0);
        }

        public Currency AddAmount(int amount)
        {
            Amount += amount;
            Amount = Mathf.Max(Amount, 0);
            return this;
        }
    }
}