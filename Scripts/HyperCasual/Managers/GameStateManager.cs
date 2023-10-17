using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DTT.Utils.Extensions;
using IngameDebugConsole;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public class GameStateManager : SubManager
    {
        public GameState GameState;
        [SerializeField] private List<StateModule> StateModules;
        [SerializeField] private float SaveCheckFrequency = 1f;
        [SerializeField] private bool SaveData = true;
        private float _lastCheckTime;

        //Events
        public EventHandler<(string, int)> CurrencyUpdatedEvent;

        /// <summary>
        /// Creates the game state. This should be overriden for project specific game states.
        /// </summary>
        protected virtual void CreateGameState()
        {
            GameState = new GameState();

            //Register state modules
            foreach(var module in StateModules)
            {
                GameState.RegisterModule(module);
            }
        }

        public override async UniTask Initialize(GameManager gameManager)
        {
            //Subscribe to events
            await base.Initialize(gameManager);
            CreateGameState();
            await GameState.LoadData();
        }

        protected virtual void LateUpdate()
        {
            //Check periodically whether to save the game state or not. The game state won't save itself if its not dirtied
            if (GameState == null) return;
            if (!(Time.time - _lastCheckTime > SaveCheckFrequency)) return;
            if (SaveData) GameState.SaveData();
            _lastCheckTime = Time.time;
        }

        public GameState GetGameState()
        {
            return GameState;
        }

        public T GetModule<T>() where T : StateModule
        {
            return GameState.GetModule<T>();
        }

        #region Currencies
        [Button("Add Currency")]
        public virtual void AddCurrency(string currencyId, int amount)
        {
            HyperCasualGameModel hcGameModel = GameState.GetModule<HyperCasualGameModel>();
            if(hcGameModel == null) return;
            hcGameModel.AddCurrency(currencyId, amount);
            UpdateCurrency(currencyId, hcGameModel.GetCurrencyAmount(currencyId));
        }
        [ConsoleMethod("addCurrency", "Adds Currency")]
        public static void AddCurrencyCC(string currencyId, int amount)
        {
            GameState gameState = GameStateManager.GetContext<GameStateManager>().GameState;
            HyperCasualGameModel hcGameModel = gameState.GetModule<HyperCasualGameModel>();
            if (hcGameModel == null) return;
            hcGameModel.AddCurrency(currencyId, amount);
        }

        public virtual void RemoveCurrency(string currencyId, int amount)
        {
            HyperCasualGameModel hcGameModel = GameState.GetModule<HyperCasualGameModel>();
            if (hcGameModel == null) return;

            hcGameModel.RemoveCurrency(currencyId, amount);
            UpdateCurrency(currencyId, hcGameModel.GetCurrencyAmount(currencyId));
        }

        [Button("Set Currency")]
        public virtual void SetCurrency(string currencyId, int amount)
        {
            HyperCasualGameModel hcGameModel = GameState.GetModule<HyperCasualGameModel>();
            if (hcGameModel == null) return;

            hcGameModel.SetCurrency(currencyId, amount);
            UpdateCurrency(currencyId, hcGameModel.GetCurrencyAmount(currencyId));
        }

        [ConsoleMethod("setCurrency", "Sets Currency")]
        public static void SetCurrencyCC(string currencyId, int amount)
        {
            GameStateManager context = GameStateManager.GetContext<GameStateManager>();
            HyperCasualGameModel hcGameModel = context.GameState.GetModule<HyperCasualGameModel>();
            if(hcGameModel == null) return;
            hcGameModel.SetCurrency(currencyId, amount);
            (GameManager.Instance.GetSubManagerByType<GameStateManager>() as GameStateManager).SetCurrency(currencyId, amount);
        }
        public virtual Currency GetCurrency(string currencyId)
        {
            HyperCasualGameModel hcGameModel = GameState.GetModule<HyperCasualGameModel>();
            if (hcGameModel == null)
            {
                return new Currency
                {
                    Amount = 0,
                    CurrencyId = currencyId,
                };
            }
            return hcGameModel.GetCurrency(currencyId);
        }

        public static Currency GetCurrencyStatic(string currencyId)
        {
            GameStateManager context = GameStateManager.GetContext<GameStateManager>();
            HyperCasualGameModel hcGameModel = context.GameState.GetModule<HyperCasualGameModel>();
            if (hcGameModel == null)
            {
                return new Currency
                {
                    Amount = 0,
                    CurrencyId = currencyId,
                };
            }
            return hcGameModel.GetCurrency(currencyId);
        }   

        /// <summary>
        /// Fires an event, telling that the value of a currency has been updated.
        /// </summary>
        /// <param name="currencyId"></param>
        public virtual void UpdateCurrency(string currencyId, int amount)
        {
            CurrencyUpdatedEvent?.Invoke(this, (currencyId, amount));
        }

        #endregion
    }
}