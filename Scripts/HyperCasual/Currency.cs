using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public enum Currencies
    {
        Coin = 0,
        Gem = 1,
        Star = 2,
    }
    public struct Currency
    {
        public int Amount;
        public int CurrencyId;

        public Currency SetAmount(int amount)
        {
            Amount = amount;
            Amount = Mathf.Max(Amount, 0);
            return this;
        }

        public Currency AddAmount(int amount)
        {
            Amount += amount;
            Amount = Mathf.Max(Amount, 0);
            return this;
        }
    }
}