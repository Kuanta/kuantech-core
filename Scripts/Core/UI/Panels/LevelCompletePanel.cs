using Kuantech.Core.FX;
using UnityEngine.UI;

namespace Kuantech.Core.UI
{
    public class LevelCompletePanel : UIMenu
    {
        public Button ContinueButton;
        public Effect VictoryEffect;

        public void Initialize()
        {
            ContinueButton.onClick.AddListener(()=>{
                LevelManager.GetContext<LevelManager>().CompleteLevel();
            });
        }

        public override void Open()
        {
            base.Open();
            if(VictoryEffect != null)
            {
                VictoryEffect.Play();
            }
        }
    }
}