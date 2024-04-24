using Kuantech.HyperCasual.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Kuantech.Puzzle.UI
{
    public class BoosterActivateButton : MonoBehaviour
    {
        public PuzzleBooster Booster;
        [SerializeField] private Button Button;
        [SerializeField] private CurrencyIndicator CurrencyIndicator;
        private UnityAction _onClickedAction; 

        public void Initialize(UnityAction onClickedAction)
        {
            _onClickedAction = onClickedAction;
            CurrencyIndicator.SetCurrency(Booster.PriceCurrencyType);
            CurrencyIndicator.SetAmount(Booster.Price);
            if (Button == null)
            {
                Button = GetComponent<Button>();
            }

            if (Button == null) return;
            Button.onClick.AddListener(OnClicked);
        }
 
        private void OnClicked()
        {
            _onClickedAction?.Invoke();
        }
    }
}