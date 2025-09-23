using UnityEngine;
namespace Kuantech.Puzzle
{
    public class BoardTileBackground : MonoBehaviour
    {
        [Header("Base Color")] [SerializeField]
        private string BaseColorKey = "_BaseColor";
        public float MaskedOpacity = 0.5f;
        public float UnmaskedOpacity = 1f;
        
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

        public void SetMasked(bool masked)
        {
            Color baseColor = HighlightRenderer.material.GetColor(BaseColorKey);
            baseColor.a = masked ? MaskedOpacity : UnmaskedOpacity;
            HighlightRenderer.material.SetColor(BaseColorKey, baseColor);
            gameObject.SetActive(!masked);

        }
        public virtual void SetColor(Color color)
        {
            HighlightRenderer.material.SetColor(BaseColorKey, color);
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