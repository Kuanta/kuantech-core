using Kuantech.Core;

namespace Kuantech.ArcadeIdle
{
    public class PlayerWallet : ActorWallet
    {
        public override void Initialize()
        {
            base.Initialize();
            //Load wallet
            GameStateManager gsm = GameStateManager.GetContext<GameStateManager>();
            if (gsm == null) return;
            CurrencyModel currModel = gsm.GetGameState().GetModule<CurrencyModel>();
            foreach(var pair in currModel.Data.Currencies)
            {
                DepositCurrency(pair.Key, pair.Value.Amount);
            }
        }
        public override int GetCurrencyAmount(string currencyId)
        {
            GameStateManager gsm = GameStateManager.GetContext<GameStateManager>();
            if (gsm == null) return base.GetCurrencyAmount(currencyId);
            int held = gsm.GetCurrency(currencyId).Amount;
            HeldCurrencies[currencyId] = held;
            return held;
        }

        /// <summary>
        /// Override dirty state to do nothing because the currencies are saved using CurrencyModule
        /// </summary>
        public override void DirtyState()
        {

        }
    }
}