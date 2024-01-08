using Kuantech.Core;
using Kuantech.HyperCasual.UI;
using Kuantech.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.ArcadeIdle.UI
{
    public class HireWorkerButton : MonoBehaviour
    {
        [Header("Worker Info")]
        [KTTag("CharacterTag")]
        [SerializeField] private int WorkerTag;

        [Header("Visuals")]
        [SerializeField] private Button BuyButton;
        [SerializeField] private CurrencyIndicator CurrencyIndicator;

        private void Start()
        {
            BuyButton.onClick.AddListener(OnBuyButtonClicked);
        }

        private void OnEnable() {
            UpdateUI();    
        }

        private void OnBuyButtonClicked()
        {
            if(ArcadeIdleManager.HireWorker(WorkerTag))
            {
                UpdateUI();
            }
        }

        private void UpdateUI()
        {
            ArcadeIdleManager aim =  ArcadeIdleManager.GetContext<ArcadeIdleManager>();
            int price = ArcadeIdleManager.GetWorkerHirePrice(WorkerTag);
            CurrencyIndicator.SetCurrency(aim.NpcHireCurrency);
            CurrencyIndicator.SetAmount(price);

            CurrencyModel cm = GameStateManager.GetModuleStatic<CurrencyModel>();
            int heldAmount = cm.GetCurrencyAmount(aim.NpcHireCurrency.CurrencyId);
            BuyButton.interactable = heldAmount >= price;
        }
    }
}