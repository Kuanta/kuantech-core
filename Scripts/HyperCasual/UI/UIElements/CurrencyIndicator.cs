using Kuantech.Core;
using Kuantech.Core.Store;
using Kuantech.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Kuantech.HyperCasual.UI
{
    public class CurrencyIndicator : MonoBehaviour
    {
        public bool AutoUpdate = false;
        [FormerlySerializedAs("currencyAsset")] public CurrencyAsset CurrencyAsset;

        [SerializeField] private Image CurrencyIcon;
        [SerializeField] private TMP_Text CurrencyAmount;

        protected bool Initialized = false;

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
            Sprite currIcon = this.CurrencyAsset.Icon;
            if (currIcon != null && CurrencyIcon != null)
            {
                CurrencyIcon.sprite = currIcon;
            }
        }
        
        protected virtual void Initialize()
        {
            if (!CanGetCurrency()) return;

            if (!AutoUpdate) return;
            var cm = CurrencyManager.GetContext<CurrencyManager>();
            if (cm == null) return;
            cm.CurrencyUpdated += OnCurrencyChangeEvent;
            // gsm.CurrencyUpdatedEvent += OnCurrencyChangeEvent;
            UpdateValue();
            Initialized = true;
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
            CurrencyAmount.text = amount.Stringfy();
        }

        /// <summary>
        /// Returns the currency id
        /// </summary>
        /// <returns></returns>
        public virtual string GetCurrencyId()
        {
            return CurrencyAsset.GetId();
        }
    }
}