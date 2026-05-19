using UnityEngine;

namespace Kuantech.Puzzle
{
    public class RendererBasedTileHighlighter : TileHighlighter
    {
        [SerializeField] protected Renderer HighlightRenderer;
        public float MaskedOpacity = 0.5f;
        public float UnmaskedOpacity = 1f;
        private string BaseColorKey = "_BaseColor";
        [SerializeField] private string HighlightToggleFieldKey = "_HighlightToggle";

        public override void ClearHighlight()
        {
            if (HighlightRenderer == null) return;
            if (HighlightRenderer.material.HasProperty(HighlightToggleFieldKey))
            {
                HighlightRenderer.material.SetFloat(HighlightToggleFieldKey, 0);
            }
        }

        public override void Highlight()
        {
            if (HighlightRenderer == null) return;
            if (HighlightRenderer.material.HasProperty(HighlightToggleFieldKey))
            {
                HighlightRenderer.material.SetFloat(HighlightToggleFieldKey, 1);
            }
        }

        public override void SetMasked(bool masked)
        {

            if (HighlightRenderer == null) return;
            if (HighlightRenderer.material.HasProperty(BaseColorKey))
            {
                Color baseColor = HighlightRenderer.material.GetColor(BaseColorKey);
                baseColor.a = masked ? MaskedOpacity : UnmaskedOpacity;
                HighlightRenderer.material.SetColor(BaseColorKey, baseColor);
            }
        }

        public virtual void SetColor(Color color)
        {
            HighlightRenderer.material.SetColor(BaseColorKey, color);
        }
    }
}