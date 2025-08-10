using Kuantech.Core;
using Kuantech.Core.Store;

namespace Kuantech.Midcore.Tutorial
{
    public class EnsureCurrencyTask : GameTask
    {
        public CurrencyAsset CurrencyToEnsure;
        public int MinAmount;

        public override void StartTask()
        {
            int currentAmount = CurrencyManager.GetCurrencyAmount(CurrencyToEnsure);
            if (currentAmount < MinAmount)
            {
                CurrencyManager.SetCurrency(CurrencyToEnsure, MinAmount);
            }
            base.StartTask();
            CompleteTask();
        }
    }
}