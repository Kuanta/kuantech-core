using Kuantech.Core;
using Kuantech.Core.FX;
using Kuantech.Core.UI;
using TMPro;
using UnityEngine.UI;

namespace Kuantech.Core.UI
{
    public class LevelFailPanel : UIMenu
    {
        public TMP_Text FailMessageText;
        public Button RestartButton;
        public Effect LoseEffect;
        public virtual void Initialize()
        {
            RestartButton.onClick.AddListener(() =>
            {
                LevelManager.GetContext<LevelManager>().RestartLevel();
            });
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
    }
}