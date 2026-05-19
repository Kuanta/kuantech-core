using UnityEditor;
using UnityEngine;
namespace Kuantech.Puzzle
{
    public class BoardTileBackground : MonoBehaviour
    {
        [Header("Highlighting")] 
        [SerializeField] private TileHighlighter Highlighter;

        [Header("Even & Odd")]
        [SerializeField] private GameObject EvenBackground;
        [SerializeField] private GameObject OddBackground;
        
        public void SetBackground(int row, int col)
        {
            bool isEven = (row + col) % 2 == 0;
            EvenBackground.SetActive(isEven);
            OddBackground.SetActive(!isEven);
        }

        public void SetMasked(bool masked)
        {
            if (Highlighter != null)
            {
                Highlighter.SetMasked(masked);
            }

        }
   
        public virtual void Highlight()
        {
            if(Highlighter != null)
            {
                Highlighter.ToggleHighlight(true);
            }
        }

        public virtual void ClearHighlight()
        {
            if (Highlighter != null)
            {
                Highlighter.ToggleHighlight(false);
            }
        }
        
        /// <summary>
        /// Indicate is used in tutorials. Used to grab focus of players towards the tile. Not same as highlight
        /// </summary>
        public virtual void Indicate()
        {
            
        }
        
        /// <summary>
        /// Clears the indicate
        /// </summary>
        public virtual void ClearIndicate()
        {
            
        }
    }
}