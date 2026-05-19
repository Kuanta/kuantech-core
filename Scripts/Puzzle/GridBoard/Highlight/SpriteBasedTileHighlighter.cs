using UnityEngine;

namespace Kuantech.Puzzle
{
    public class SpriteBasedTileHighlighter : TileHighlighter
    {
        public SpriteRenderer Image;
        public Color ClearColor;
        public Color HighlightColor;
        public Color MaskedColor;
        public override void ClearHighlight()
        {
            if (Image != null) Image.color = ClearColor;
        }

        public override void Highlight()
        {
            if (Image != null) Image.color = HighlightColor;
        }

        public override void SetMasked(bool masked)
        {
            if (Image != null) Image.color = masked ? MaskedColor : ClearColor;
        }
    }
}