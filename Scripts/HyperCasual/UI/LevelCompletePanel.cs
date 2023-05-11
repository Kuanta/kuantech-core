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
        [SerializeField] private Effect ShowEffect;

        public override void Show()
        {
            base.Show();
            if(ShowEffect != null) ShowEffect.Play();
        }
        protected override void Start()
        {
            base.Start();
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