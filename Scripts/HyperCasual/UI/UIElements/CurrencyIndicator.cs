using Kuantech.Utils;
using TMPro;
using UnityEngine;

namespace Kuantech.Core.HyperCasual.UI
{
    public class CurrencyIndicator : MonoBehaviour
    {
        public int CurrencyId;
        [SerializeField] private TMP_Text CurrencyAmount;

        private void Start()
        {
            GameStateManager gsm = (GameManager.Instance.GetSubManagerByType<GameStateManager>() as GameStateManager);
            if(gsm == null) return;
            gsm.CurrencyUpdatedEvent += OnCurrencyChangeEvent;
        }

        /// <summary>
        /// Gets the currency value on enable, if events are missed while inactive
        /// </summary>
        private void OnEnable()
        {
            //Get the current currency value
            GameStateManager gsm = (GameManager.Instance.GetSubManagerByType<GameStateManager>() as GameStateManager);
            if (gsm == null) return;
            int amount = gsm.GetCurrency(CurrencyId).Amount;
            SetAmount(amount);
        }

        private void OnCurrencyChangeEvent(object sender, (int, int) val)
        {
            if(val.Item1 == CurrencyId)
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
            GameStateManager gsm = (GameManager.Instance.GetSubManagerByType<GameStateManager>() as GameStateManager);
            if (gsm == null) return;
            gsm.CurrencyUpdatedEvent -= OnCurrencyChangeEvent;
        }
    }
}