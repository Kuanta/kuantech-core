using Kuantech.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Core.HyperCasual
{
    public class LevelCompletePanel : UIMenu
    {
        [SerializeField] private TMP_Text EarnedCoinsText;
        [SerializeField] private Button CompleteLevelButton;

        private void Start()
        {
            CompleteLevelButton.onClick.AddListener(OnCompleteLevelButton);
        }

        public void SetEarnings(int earnedCoins)
        {
            EarnedCoinsText.text = earnedCoins.ToString();
        }
        private void OnCompleteLevelButton()
        {
            ((HCGameManager)GameManager.Instance).CompleteLevel();
        }
    }
}