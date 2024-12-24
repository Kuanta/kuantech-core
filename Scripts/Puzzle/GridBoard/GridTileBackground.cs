using UnityEngine;
namespace Kuantech.Puzzle
{
    public class GridTileBackground : MonoBehaviour
    {
        [Header("Highlighting")] 
        [SerializeField] protected Renderer HighlightRenderer;
        [SerializeField] private string HighlightToggleFieldKey = "_HighlightToggle";
        
        [Header("Even & Odd")]
        [SerializeField] private GameObject EvenBackground;
        [SerializeField] private GameObject OddBackground;
        
        public void SetBackground(int row, int col)
        {
            bool isEven = (row + col) % 2 == 0;
            EvenBackground.SetActive(isEven);
            OddBackground.SetActive(!isEven);
        }

        public virtual void Highlight()
        {
            if (HighlightRenderer == null) return;
            HighlightRenderer.material.SetFloat(HighlightToggleFieldKey, 1);
        }

        public virtual void ClearHighlight()
        {
            if (HighlightRenderer == null) return;
            HighlightRenderer.material.SetFloat(HighlightToggleFieldKey, 0);
        }
    }
}