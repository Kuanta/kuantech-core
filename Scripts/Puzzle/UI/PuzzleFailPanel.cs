using Kuantech.Core;
using Kuantech.Core.FX;
using Kuantech.UI;
using UnityEngine.UI;

namespace Kuantech.Puzzle.UI
{
    public class PuzzleFailPanel : UIMenu
    {
        public Button RestartButton;
        public Effect LoseEffect;
        public void Initialize(PuzzleLevelUI parentUI)
        {
            RestartButton.onClick.AddListener(() =>
            {
                LevelManager.GetContext<LevelManager>().RestartLevel();
            });
        }

        public override void Show()
        {
            base.Show();
            if (LoseEffect != null)
            {
                LoseEffect.Play();
            }
        }
    }
}