using Kuantech.Utils;
using TMPro;
using UnityEngine;

namespace Kuantech.Core.HyperCasual.UI
{
    public class CurrencyIndicator : MonoBehaviour
    {
        public bool AutoUpdate = false;
        [SerializeField] private int CurrencyId;
        [SerializeField] private TMP_Text CurrencyAmount;

        protected bool Initialized = false;

        public bool CanGetCurrency()
        {
            if (!AutoUpdate || !GameManager.InstanceExists()) return false;
            // GameStateManager gsm = (GameManager.Instance.GetSubManagerByType<GameStateManager>() as GameStateManager);
            // if (gsm == null) return false;
            return true;
        }
        protected virtual void Start()
        {
            if(CanGetCurrency()) Initialize();
        }

        protected virtual void Initialize()
        {
            if(!CanGetCurrency()) return;
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
            if (!Initialized)
            {
                Initialize();
                return;
            }
        }
        protected virtual void UpdateValue()
        {
            //Get the current currency value
            GameStateManager gsm = (GameManager.Instance.GetSubManagerByType<GameStateManager>() as GameStateManager);
            if (gsm == null) return;
            int amount = gsm.GetCurrency(GetCurrencyId()).Amount;
            SetAmount(amount);
        }
        private void OnCurrencyChangeEvent(object sender, (int, int) val)
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
        public virtual int GetCurrencyId()
        {
            return CurrencyId;
        }
    }
}