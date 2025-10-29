using UnityEngine;

namespace Kuantech.Puzzle
{
    public class SpriteBoardTileBackground : BoardTileBackground
    {
        public Color HighlightColor = Color.green;
        private Color _lastColor;

        public GameObject IndicateObject;
        
        public override void Highlight()
        {
            _lastColor = ((SpriteRenderer) HighlightRenderer).color;
            ((SpriteRenderer) HighlightRenderer).color = HighlightColor;
        }

        public override void ClearHighlight()
        {
            ((SpriteRenderer) HighlightRenderer).color = _lastColor;
        }

        public override void Indicate()
        {
            if (IndicateObject != null) IndicateObject.SetActive(true);
        }

        public override void ClearIndicate()
        {
            if (IndicateObject != null) IndicateObject.SetActive(false);
        }
    }
}