using UnityEngine;

namespace Kuantech.Puzzle.UI
{
    public class StageIndicatorElement : MonoBehaviour
    {
        public GameObject FillElement;
        public GameObject ConnectionVisual;
        
        public void SetFill(bool fill)
        {
            if (FillElement == null) return;
            FillElement.gameObject.SetActive(fill);
        }

        public void ToggleConnection(bool toggle)
        {
            ConnectionVisual.SetActive(toggle);
        }
    }
}