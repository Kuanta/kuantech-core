using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kuantech.Core;
using Kuantech.Core.Store;
using Kuantech.HyperCasual.UI;

namespace Kuantech.ArcadeIdle.UI
{
    public class ArcadeIdleUIManager : SubManager
    {
        public List<ArcadeIdlePanel> Panels;
        public List<CurrencyIndicator> CurrencyIndicatorsList;
        public Dictionary<CurrencyAsset, CurrencyIndicator> CurrencyIndicators;
        private Dictionary<string, ArcadeIdlePanel> _panelsById;

        public override async UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);
            _panelsById = new Dictionary<string, ArcadeIdlePanel>();
            foreach(var panel in Panels)
            {
                _panelsById[panel.MenuId] = panel;
            }
            if(CurrencyIndicatorsList == null) return;
            CurrencyIndicators = new Dictionary<CurrencyAsset, CurrencyIndicator>();
            foreach(var indicator in CurrencyIndicatorsList)
            {
                CurrencyIndicators[indicator.CurrencyAsset] = indicator;
            }
        }

        public ArcadeIdlePanel GetPanel(string panelId)
        {
            if(_panelsById == null || !_panelsById.ContainsKey(panelId)) return null;
            return _panelsById[panelId];
        }

        public static void OpenPanel(string panelId)
        {
            ArcadeIdleUIManager context = GetContext<ArcadeIdleUIManager>();
            if(context == null) return;
            context.GetPanel(panelId).Open();
        }
    }
}