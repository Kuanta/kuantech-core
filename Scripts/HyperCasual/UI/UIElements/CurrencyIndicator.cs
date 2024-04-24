using Kuantech.Core;
using Kuantech.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.HyperCasual.UI
{
    public class CurrencyIndicator : MonoBehaviour
    {
        public bool AutoUpdate = false;
        public CurrencyData CurrencyData;

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
            SetCurrency(CurrencyData);
            if (CanGetCurrency()) Initialize();
        }

        public void SetCurrency(CurrencyData currencyData)
        {
            if(currencyData == null) return;
            CurrencyData = currencyData;
            Sprite currIcon = CurrencyData.CurrencyIcon;
            if (currIcon != null && CurrencyIcon != null)
            {
                CurrencyIcon.sprite = currIcon;
            }
        }
        
        protected virtual void Initialize()
        {
            if (!CanGetCurrency()) return;

            if (!AutoUpdate) return;
            GameStateManager gsm = (GameManager.Instance.GetSubManagerByType<GameStateManager>() as GameStateManager);
            if (gsm == null) return;
            gsm.CurrencyUpdatedEvent += OnCurrencyChangeEvent;
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
            //Get the current currency value
            GameStateManager gsm = (GameManager.Instance.GetSubManagerByType<GameStateManager>() as GameStateManager);
            if (gsm == null) return;
            int amount = gsm.GetCurrency(GetCurrencyId()).Amount;
            SetAmount(amount);
        }
        private void OnCurrencyChangeEvent(object sender, (string, int) val)
        {
            if(val.Item1 == GetCurrencyId())
            {
                SetAmount(val.Item2);
            }
        }

        public virtual void SetAmount(int amount)
        {
            CurrencyAmount.text = amount.Stringfy();
        }

        /// <summary>
        /// Unsubscribe from event to prevent dangling event subscriptions
        /// </summary>
        private void OnDestroy()
        {
            if (!AutoUpdate) return;
            GameStateManager gsm = (GameManager.Instance.GetSubManagerByType<GameStateManager>() as GameStateManager);
            if (gsm == null) return;
            gsm.CurrencyUpdatedEvent -= OnCurrencyChangeEvent;
        }

        /// <summary>
        /// Returns the currency id
        /// </summary>
        /// <returns></returns>
        public virtual string GetCurrencyId()
        {
            return CurrencyData.CurrencyId;
        }
    }
}