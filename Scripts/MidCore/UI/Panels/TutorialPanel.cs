using Kuantech.Core.UI;
using Kuantech.Puzzle.UI;
using UnityEngine;

namespace Kuantech.Midcore.UI
{
    public class TutorialPanel : UIElement
    {
        [Header("Tutorial Text")] [SerializeField]
        private TextBox TutorialText;

        [Header("Tutorial Hand")] [SerializeField]
        private TutorialHand TutorialHand;

        #region Text Box
        public void SetTutorialText(string text)
        {
            if (TutorialText == null) return;
            TutorialText.SetText(text);
        }

        public void ToggleTutorialText(bool toggle)
        {
            if (TutorialText == null) return;
            TutorialText.gameObject.SetActive(toggle);
        }

        #endregion


        #region Tutorial Hand

        public TutorialHand GetTutorialHand()
        {
            return TutorialHand;
        }

        #endregion
    }
}