using Kuantech.Core;
using Kuantech.Core.FX;
using Kuantech.Core.UI;
using TMPro;
using UnityEngine.UI;

namespace Kuantech.Midcore.UI
{
    public class FailPanel : UIElement
    {
        public TMP_Text FailMessageText;
        public Button RestartButton;
        public Effect LoseEffect;
        
        public virtual void Initialize(LevelUI parentUI)
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
    }
}