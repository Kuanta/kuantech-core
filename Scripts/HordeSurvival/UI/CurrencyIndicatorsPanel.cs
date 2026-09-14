using System.Collections.Generic;
using Kuantech.Core.Store;
using Kuantech.Core.UI;
using Kuantech.HyperCasual.UI;
using UnityEngine;

namespace Kuantech.HordeSurvival
{
    /// <summary>
    /// In-run currency readout. Scans its child CurrencyIndicators once and drives them from the run's
    /// PENDING rewards, not the player's wallet — a run's earnings are only banked into CurrencyManager
    /// when it ends, so during play the wallet has not moved yet.
    ///
    /// Its indicators must have AutoUpdate off. With it on they bind themselves to CurrencyManager and
    /// would show the banked balance, fighting this panel over the same text.
    /// </summary>
    public class CurrencyIndicatorsPanel : KtUIPanel
    {
        private readonly Dictionary<string, CurrencyIndicator> _indicatorsById = new();
        private HordeSurvivalRunHandler _runHandler;

        public override void Initialize()
        {
            base.Initialize();

            _indicatorsById.Clear();
            // Include inactive: an indicator for a currency this arena rarely drops may start hidden.
            foreach (var indicator in GetComponentsInChildren<CurrencyIndicator>(true))
            {
                if (indicator == null) continue;
                string currencyId = indicator.GetCurrencyId();
                if (string.IsNullOrEmpty(currencyId))
                {
                    Debug.LogWarning($"CurrencyIndicatorsPanel ({name}): an indicator has no CurrencyAsset — it will never update.");
                    continue;
                }

                indicator.Initialize();
                _indicatorsById[currencyId] = indicator;
            }

            _runHandler = HordeSurvivalRunHandler.GetContext<HordeSurvivalRunHandler>();
            if (_runHandler != null)
            {
                _runHandler.OnRewardsChanged -= Refresh;
                _runHandler.OnRewardsChanged += Refresh;
                // A restart wipes the pending rewards, so the counters must go back to zero with it.
                _runHandler.OnRunStarted -= Refresh;
                _runHandler.OnRunStarted += Refresh;
            }

            Refresh();
        }

        /// <summary>Re-reads every counter from the run's pending rewards.</summary>
        public void Refresh()
        {
            if (_runHandler == null) return;
            foreach (var indicator in _indicatorsById.Values)
            {
                if (indicator == null) continue;
                indicator.SetAmount(_runHandler.GetPendingCurrency(indicator.CurrencyAsset));
            }
        }

        /// <summary>
        /// Updates a single counter. Not needed for the normal flow — collecting currency raises
        /// OnRewardsChanged and this panel refreshes itself — but available for anything that changes a
        /// currency outside that path.
        /// </summary>
        public void UpdateCurrency(CurrencyAsset currency)
        {
            if (_runHandler == null || currency == null) return;
            if (!_indicatorsById.TryGetValue(currency.GetId(), out CurrencyIndicator indicator)) return;
            if (indicator != null) indicator.SetAmount(_runHandler.GetPendingCurrency(currency));
        }

        private void OnDestroy()
        {
            if (_runHandler == null) return;
            _runHandler.OnRewardsChanged -= Refresh;
            _runHandler.OnRunStarted -= Refresh;
        }
    }
}
