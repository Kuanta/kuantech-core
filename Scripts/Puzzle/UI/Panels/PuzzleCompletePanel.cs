using Kuantech.Core;
using Kuantech.Core.FX;
using Kuantech.UI;
using UnityEngine.UI;

namespace Kuantech.Puzzle.UI
{
    public class PuzzleCompletePanel : UIMenu
    {
        public PuzzleLevelUI ParentUI;
        public Button ContinueButton;
        public Effect VictoryEffect;

        public void Initialize(PuzzleLevelUI parentUI)
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