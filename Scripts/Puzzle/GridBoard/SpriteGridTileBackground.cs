using UnityEngine;

namespace Kuantech.Puzzle
{
    public class SpriteGridTileBackground : GridTileBackground
    {
        public Color HighlightColor = Color.green;
        private Color _lastColor;
        public override void Highlight()
        {
            _lastColor = ((SpriteRenderer) HighlightRenderer).color;
            ((SpriteRenderer) HighlightRenderer).color = HighlightColor;
        }

        public override void ClearHighlight()
        {
            ((SpriteRenderer) HighlightRenderer).color = _lastColor;
        }
    }
}