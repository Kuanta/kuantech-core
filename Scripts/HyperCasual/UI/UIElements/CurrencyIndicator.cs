using Kuantech.Core;
using Kuantech.Core.Store;
using Kuantech.Core.UI;
using Kuantech.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.HyperCasual.UI
{
    public class CurrencyIndicator : UIElement
    {
        public bool AutoUpdate = false;
        public CurrencyAsset CurrencyAsset;

        [SerializeField] private Image CurrencyIcon;
        [SerializeField] private TMP_Text CurrencyAmount;

        public bool CanGetCurrency()
        {
            if (!AutoUpdate || !GameManager.InstanceExists()) return false;
            return true;
        }
        protected virtual void Start()
        {
            //Set currency icon
            SetCurrency(CurrencyAsset);
            if (CanGetCurrency()) Initialize();
        }

        public void SetCurrency(CurrencyAsset currencyAsset)
        {
            if(currencyAsset == null) return;
            this.CurrencyAsset = currencyAsset;
            Sprite currIcon = this.CurrencyAsset.GetIcon();
            if (currIcon != null && CurrencyIcon != null)
            {
                CurrencyIcon.sprite = currIcon;
            }
        }
        
        public override void Initialize()
        {
            if (Initialized) return;
            base.Initialize();
            if (!CanGetCurrency()) return;

            if (!AutoUpdate) return;
            var cm = CurrencyManager.GetContext<CurrencyManager>();
            if (cm == null) return;
            cm.CurrencyUpdated -= OnCurrencyChangeEvent;
            cm.CurrencyUpdated += OnCurrencyChangeEvent;
            // gsm.CurrencyUpdatedEvent += OnCurrencyChangeEvent;
            UpdateValue();
        }

        /// <summary>
        /// Gets the currency value on enable, if events are missed while inactive
        /// </summary>
        private void OnEnable()
        {
            if(!CanGetCurrency()) return;
            UpdateValue();
        }

        private void Update()
        {
            if (Initialized) return;
            Initialize();
            return;
        }
        public virtual void UpdateValue()
        {
            int amount = CurrencyManager.GetCurrencyAmount(CurrencyAsset);
            SetAmount(amount);
        }
        
        private void OnCurrencyChangeEvent(CurrencyData data)
        {
            if(data.CurrencyId== GetCurrencyId())
            {
                SetAmount(data.CurrencyAmount);
            }
        }

        public virtual void SetAmount(int amount)
        {
            if (CurrencyAsset == null) return;
            CurrencyAmount.text = amount.Stringfy();
        }

        /// <summary>
        /// Returns the currency id
        /// </summary>
        /// <returns></returns>
        public virtual string GetCurrencyId()
        {
            return CurrencyAsset != null ? CurrencyAsset.GetId() : "";
        }
    }
}