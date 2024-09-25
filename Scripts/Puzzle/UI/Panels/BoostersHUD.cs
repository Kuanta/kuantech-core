using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Puzzle.UI
{
    public class BoostersHUD : MonoBehaviour
    {
        [Header("Booster Buttons")]
        public List<BoosterActivateButton> BoosterButtons;

        [Header("UI Elements")] 
        [SerializeField] private GameObject BoosterInstructionsParent;
        [SerializeField] private GameObject BoosterButtonsParent;

        [SerializeField] private TMP_Text BoosterInstruction;
        [SerializeField] private Button CancelBoosterButton;
         
        [Header("")]
         
        private PuzzleLevelUI _levelUI;
        public void Initialize(PuzzleLevelUI levelUI)
        {
            _levelUI = levelUI;
            foreach (var button in BoosterButtons)
            {
                button.Initialize(OnBoosterActivated);
            }
            CancelBoosterButton.onClick.AddListener(OnCancelBoosterButtonClicked);
        }

        public void OnLevelSetup(PuzzleLevel level)
        {
            foreach (var button in BoosterButtons)
            {
                button.HandleLockedState(level.LevelNumber);
            }
        }
        
        public void OnBoosterActivated(BoosterActivateButton button)
        {
            if (_levelUI == null || button.booster == null) return;
            if(!_levelUI.CurrentLevel.ActivateBooster(button.booster)) return;

            if (!button.booster.ShowUIOnActivation) return;
            //Todo: Implement animations here
            _levelUI.SetUIForBooster(button.booster);
            BoosterInstructionsParent.SetActive(true);
            BoosterButtonsParent.SetActive(false);
            BoosterInstruction.text = button.booster.Description;
            _levelUI.SetUIForBooster(button.booster);
        }

        public void OnBoosterDeactivated()
        {
            BoosterInstructionsParent.SetActive(false);
            BoosterButtonsParent.SetActive(true);
        }
        
        private void OnCancelBoosterButtonClicked()
        {
            _levelUI.CurrentLevel.CancelCurrentBooster();
        }

        public void Reset()
        {
            BoosterInstructionsParent.SetActive(false);
            BoosterButtonsParent.SetActive(true);
        }
    }
}