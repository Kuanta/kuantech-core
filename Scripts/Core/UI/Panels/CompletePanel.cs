using Kuantech.Core.FX;
using Kuantech.Midcore.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Core.UI
{
    public class CompletePanel : UIMenu
    {
        [Header("Components")] 
        public RewardsPanel RewardsPanel;
        public Button ContinueButton;
        
        
        public Effect VictoryEffect;

        public override void Initialize()
        {
            if (Initialized) return;
            base.Initialize();
            ContinueButton.onClick.AddListener(OnCompleteLevelButton);
        }

        public override void Open()
        {
            base.Open();
            if(VictoryEffect != null)
            {
                VictoryEffect.Play();
            }
        }
        
        public virtual void OnCompleteLevelButton()
        {
      
        }
    }
}