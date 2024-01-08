using System;
using System.Collections.Generic;
using Kuantech.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    [Serializable]
    public class WalletState : ActorModuleState
    {
        public Dictionary<string, int> HeldCurrencies = new Dictionary<string, int>();
    }

    public class ActorWallet : ActorModule
    {
        //Events
        public Action<(string, int)> OnCurrencyAdded;
        public Action<(string, int)> OnCurrencySet;
        public Action<(string, int)> OnCurrencyRemoved;
        public Dictionary<string, int> HeldCurrencies = new Dictionary<string, int>();

        public override void LoadState(ActorModuleState moduleState)
        {
            base.LoadState(moduleState);
            WalletState walletState = moduleState as WalletState; //todo: Is this by reference
            HeldCurrencies = walletState.HeldCurrencies; //Create a copy
        }
        
        protected override ActorModuleState InstantiateState()
        {
            return new WalletState(){
                ModuleId = ModuleId,
                HeldCurrencies = HeldCurrencies,
                };
        }

        public virtual int GetCurrencyAmount(string currencyId)
        {
            if(HeldCurrencies == null || !HeldCurrencies.ContainsKey(currencyId)) return 0;
            return HeldCurrencies[currencyId];
        }
        [Button("Deposit Currency")]
        public virtual void DepositCurrency(string currencyId, int amount,bool fireEvent = true)
        {
            if(!HeldCurrencies.ContainsKey(currencyId))
            {
                HeldCurrencies[currencyId] = 0;
            }
            HeldCurrencies[currencyId] += amount;
            DirtyState();
            if (fireEvent) OnCurrencyAdded?.Invoke((currencyId, amount));
        }

        /// <summary>
        /// Tries to withdraw currency from the wallet.
        /// </summary>
        /// <param name="currencyId"></param>
        /// <param name="amount"></param>
        /// <returns>Amount of actually withdrawn amount. 
        /// If existing amount is less than the desired withdrawal amount, it will return the existing amount
        /// </returns>
        public virtual int WithdrawCurrency(string currencyId, int amount, bool fireEvent = true)
        {
            if(!HeldCurrencies.ContainsKey(currencyId)) return 0;
            int existingAmount = GetCurrencyAmount(currencyId);
            int amountToDraw = Mathf.Min(existingAmount, amount);
            RemoveCurrency(currencyId, amountToDraw);
            DirtyState();
            if (fireEvent) OnCurrencyRemoved?.Invoke((currencyId, amountToDraw)); //Fire the event
            return amountToDraw;
        }

        /// <summary>
        /// Sets the currency
        /// </summary>
        /// <param name="currencyId"></param>
        /// <param name="amount"></param>
        public virtual void SetCurrency(string currencyId, int amount, bool fireEvent = true)
        {
            if (!HeldCurrencies.ContainsKey(currencyId))
            {
                HeldCurrencies[currencyId] = 0;
            }
            HeldCurrencies[currencyId] = amount;
            DirtyState();
            if (fireEvent) OnCurrencySet?.Invoke((currencyId, amount));
        }

        protected virtual void RemoveCurrency(string currencyId, int amount)
        {
            HeldCurrencies[currencyId] -= amount;
        }
        public override void Reset()
        {

        }
    }
}