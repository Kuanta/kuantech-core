using UnityEngine;

namespace Kuantech.Puzzle
{
    public abstract class TileHighlighter : MonoBehaviour
    {
        public  void ToggleHighlight(bool toggle)
        {
            if(toggle)
            {
                Highlight();
            }
            else
            {
                ClearHighlight();
            }
        }
        public abstract void SetMasked(bool masked);
        public abstract void Highlight();
        public abstract void ClearHighlight();
    }
}