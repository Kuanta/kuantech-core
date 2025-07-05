using Kuantech.Core.FX;
using Kuantech.Midcore.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Core.UI
{
    public class LevelFailPanel : UIMenu
    {
        [Header("Components")]
        public RewardsPanel RewardsPanel;
        [SerializeField] private TMP_Text FailMessageText;
        [SerializeField] private Button RestartButton;
        [SerializeField] private Effect LoseEffect;
        
        public virtual void Initialize()
        {
            if (RestartButton != null)
            {
                RestartButton.onClick.AddListener(OnRestartButtonClicked);
            }
   
        }

        public override void Open()
        {
            base.Open();
            if (LoseEffect != null)
            {
                LoseEffect.Play();
            }
        }

        public void SetFailText(string failText)
        {
            if (FailMessageText == null) return;
            FailMessageText.text = failText;
        }

        protected virtual void OnRestartButtonClicked()
        {
            LevelManager.GetContext<LevelManager>().RestartLevel();
        }
    }
}