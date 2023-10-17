using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public struct Currency
    {
        public int Amount;
        public string CurrencyId;

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