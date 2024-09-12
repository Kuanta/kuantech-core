using Kuantech.HyperCasual.UI;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Kuantech.Puzzle.UI
{
    public class BoosterActivateButton : MonoBehaviour
    {
        public PuzzleBooster booster;
        [SerializeField] private Image BoosterIcon;
        [SerializeField] private Button Button;
        [SerializeField] private CurrencyIndicator CurrencyIndicator;
        private UnityAction<BoosterActivateButton> _onClickedAction;

        [Header("Locked")] 
        [SerializeField] private GameObject LockedGameObject;
        [SerializeField] private GameObject UnlockedGameObject;
        [SerializeField] private TMP_Text LevelRequirementText;
        private bool _locked = false;

        public void Initialize(UnityAction<BoosterActivateButton> onClickedAction)
        {
            _onClickedAction = onClickedAction;
            if (BoosterIcon != null)
            {
                BoosterIcon.sprite = booster.Icon;
            }

            if (CurrencyIndicator != null)
            {
                CurrencyIndicator.SetCurrency(booster.PriceCurrencyType);
                CurrencyIndicator.SetAmount(booster.Price);
            }
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
            if(LevelRequirementText != null) LevelRequirementText.text = $"Lvl. {booster.LevelRequirement}";
            SetLockedState(booster.LevelRequirement > level);
        }
        
        private void OnClicked()
        {
            if (_locked) return;
            _onClickedAction?.Invoke(this);
        }

        public void SetLockedState(bool locked)
        {
            if(LockedGameObject != null) LockedGameObject.SetActive(locked);
            if(UnlockedGameObject != null) UnlockedGameObject.SetActive(!locked);
            _locked = locked;
        }

        public bool IsLocked()
        {
            return _locked;
        }
    }
}