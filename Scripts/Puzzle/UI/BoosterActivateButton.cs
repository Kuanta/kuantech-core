using Kuantech.HyperCasual.UI;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Kuantech.Puzzle.UI
{
    public class BoosterActivateButton : MonoBehaviour
    {
        public PuzzleBooster Booster;
        [SerializeField] private Image BoosterIcon;
        [SerializeField] private Button Button;
        [SerializeField] private CurrencyIndicator CurrencyIndicator;
        private UnityAction _onClickedAction;

        [Header("Locked")] 
        [SerializeField] private GameObject LockedGameObject;
        [SerializeField] private GameObject UnlockedGameObject;
        [SerializeField] private TMP_Text LevelRequirementText;
        private bool _locked = false;

        public void Initialize(UnityAction onClickedAction)
        {
            _onClickedAction = onClickedAction;
            if (BoosterIcon != null)
            {
                BoosterIcon.sprite = Booster.Icon;
            }
            CurrencyIndicator.SetCurrency(Booster.PriceCurrencyType);
            CurrencyIndicator.SetAmount(Booster.Price);
            if (Button == null)
            {
                Button = GetComponent<Button>();
            }

            if (Button == null) return;
            Button.onClick.AddListener(OnClicked);
        }
        
        /// <summary>
        /// Decides whether the button can be clickable or not
        /// </summary>
        /// <param name="level"></param>
        public void HandleLockedState(int level)
        {
            LevelRequirementText.text = $"Lvl. {Booster.LevelRequirement}";
            SetLockedState(Booster.LevelRequirement > level);
        }
        
        private void OnClicked()
        {
            if (_locked) return;
            _onClickedAction?.Invoke();
        }

        public void SetLockedState(bool locked)
        {
            LockedGameObject.SetActive(locked);
            UnlockedGameObject.SetActive(!locked);
            _locked = locked;
        }

        public bool IsLocked()
        {
            return _locked;
        }
    }
}