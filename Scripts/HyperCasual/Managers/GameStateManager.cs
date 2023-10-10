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
        public string GameStateType;
        public GameState GameState;
        [SerializeField] private float SaveCheckFrequency = 1f;
        [SerializeField] private bool SaveData = true;
        private float _lastCheckTime;

        //Events
        public EventHandler<(int, int)> CurrencyUpdatedEvent;

        /// <summary>
        /// Creates the game state. This should be overriden for project specific game states.
        /// </summary>
        protected virtual void CreateGameState()
        {
            if(GameStateType.IsNullOrEmpty())
            {
                GameState ??= new GameState();
                return;
            }
            Type stateType = Type.GetType(GameStateType);
            if(stateType != null && stateType.IsSubclassOf(typeof(GameState)))
            {
                GameState = (GameState)Activator.CreateInstance(stateType);
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

        #region Currencies
        [Button("Add Currency")]
        public virtual void AddCurrency(int currencyId, int amount)
        {
            GameState.AddCurrency(currencyId, amount);
            UpdateCurrency(currencyId, GameState.GetCurrency(currencyId).Amount);
        }
        [ConsoleMethod("addCurrency", "Adds Currency")]
        public static void AddCurrencyCC(int currencyId, int amount)
        {
            (GameManager.Instance.GetSubManagerByType<GameStateManager>() as GameStateManager).AddCurrency(currencyId, amount);
        }

        public virtual void RemoveCurrency(int currencyId, int amount)
        {
            GameState.RemoveCurrency(currencyId, amount);
            UpdateCurrency(currencyId, GameState.GetCurrency(currencyId).Amount);
        }

        [Button("Set Currency")]
        public virtual void SetCurrency(int currencyId, int amount)
        {
            GameState.SetCurrency(currencyId, amount);
            UpdateCurrency(currencyId, GameState.GetCurrency(currencyId).Amount);
        }

        [ConsoleMethod("setCurrency", "Sets Currency")]
        public static void SetCurrencyCC(int currencyId, int amount)
        {
            (GameManager.Instance.GetSubManagerByType<GameStateManager>() as GameStateManager).SetCurrency(currencyId, amount);
        }
        public virtual Currency GetCurrency(int currencyId)
        {
            return GameState.GetCurrency(currencyId);
        }

        public static Currency GetCurrencyStatic(int currencyId)
        {
            GameStateManager context = GameManager.Instance.GetSubManagerByType<GameStateManager>() as GameStateManager;
            return context.GetCurrency(currencyId);
        }   
        /// <summary>
        /// Fires an event, telling that the value of a currency has been updated.
        /// </summary>
        /// <param name="currencyId"></param>
        public virtual void UpdateCurrency(int currencyId)
        {
            CurrencyUpdatedEvent?.Invoke(this, (currencyId, GameState.GetCurrencyAmount(currencyId)));
        }

        /// <summary>
        /// Fires an event, telling that the value of a currency has been updated.
        /// </summary>
        /// <param name="currencyId"></param>
        /// <param name="amount"></param>
        protected virtual void UpdateCurrency(int currencyId, int amount)
        {
            CurrencyUpdatedEvent?.Invoke(this, (currencyId, amount));
        }
        #endregion
    }
}