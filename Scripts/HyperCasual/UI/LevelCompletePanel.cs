using Kuantech.Core.FX;
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
        [SerializeField] protected Effect ShowEffect;

        public override void Show()
        {
            base.Show();
            if(ShowEffect != null) ShowEffect.Play();
        }

        public void Initialize()
        {
            CompleteLevelButton.onClick.AddListener(OnCompleteLevelButton);
        }
        
        public virtual void SetEarnings(int earnedCoins)
        {
            if(EarnedCoinsText != null) EarnedCoinsText.text = earnedCoins.ToString();
        }
        
        private void OnCompleteLevelButton()
        {
            ((HCGameManager)GameManager.Instance).CompleteLevel();
        }
    }
}