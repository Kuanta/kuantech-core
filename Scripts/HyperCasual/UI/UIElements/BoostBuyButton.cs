using Kuantech.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Core.HyperCasual.UI
{
    public class BoostBuyButton : MonoBehaviour {
        public string BoosterId;
        public Image CurrencyIcon;
        public TMP_Text PriceTag;
        public string LevelPrefix = "Level ";
        public TMP_Text LevelText;
        public GameObject MaxedOutIndicator;
        public GameObject CanBeUpgradedVisuals;
        private void Start()
        {
            Button button = GetComponent<Button>();
            if(button != null)
            {
                button.onClick.AddListener(OnButtonPressed);
            }
            Setup();
        }

        /// <summary>
        /// Sets the state of the button. Updates price, indicates if its maxed out or not.
        /// </summary>
        protected virtual void Setup()
        {
            BoosterManager bm = BoosterManager.GetContext<BoosterManager>();
            if(bm == null) return;
            bool maxedOut = bm.IsMaxedOut(BoosterId);
            bool canBeUpgraded = bm.CanBeUpgraded(BoosterId); //Maxed out and canBeUpgraded are different. Some boosters may require additional conditions
            MaxedOutIndicator?.SetActive(maxedOut);
            CanBeUpgradedVisuals?.SetActive(canBeUpgraded);

            int price = bm.GetBoostPrice(BoosterId);
            int currentLevel = bm.GetBoostLevel(BoosterId);
            LevelText.text = $"{LevelPrefix}{(currentLevel + 1).Stringfy()}";
            PriceTag.text = price.Stringfy();
            Sprite icon = UIResourcesManager.GetCurrencyIcon(bm.GetBoostCurrencyId(BoosterId));
            if(icon == null) return;
            CurrencyIcon.sprite = icon;
        }
        protected bool IsBoostAvailable()
        {
            return true;
        }
        private void OnButtonPressed()
        {
            BoosterManager bm = BoosterManager.GetContext<BoosterManager>();
            if (bm == null) return;
            if (!bm.BuyBooster(BoosterId)) return;
            Setup();
        }
    }
}