using UnityEngine;
using UnityEngine.UIElements;

namespace Kuantech.Core.UI
{
    /// <summary>
    /// A background that is a radial gradient: one colour in the middle, another at the edges. Used light in the middle
    /// and dark at the edges it is a gradient and a vignette at once. UI Toolkit has no gradients, so it draws a grid of
    /// vertices whose colours are worked out from their distance to the centre (no texture, any size, and the colours
    /// can be changed at run time).
    ///
    /// In UXML: <c>&lt;Kuantech.Core.UI.TkBackdrop center-color="#3B3C7A" edge-color="#0B0C1F" /&gt;</c>
    /// Anything inside it (a pattern, say) is drawn over the gradient.
    /// </summary>
    [UxmlElement]
    public partial class TkBackdrop : VisualElement
    {
        // The gradient is drawn on a grid of this many cells on each side. The colours between the vertices are
        // interpolated in straight lines, so more cells make the circle rounder.
        private const int Cells = 24;

        private Color _centerColor = new Color(0.23f, 0.24f, 0.48f);
        private Color _edgeColor = new Color(0.04f, 0.05f, 0.12f);
        private float _falloff = 1.2f;
        private float _centerY = 0.42f;

        /// <summary>The colour in the middle.</summary>
        [UxmlAttribute]
        public Color CenterColor
        {
            get => _centerColor;
            set { _centerColor = value; MarkDirtyRepaint(); }
        }

        /// <summary>The colour at the edges and corners.</summary>
        [UxmlAttribute]
        public Color EdgeColor
        {
            get => _edgeColor;
            set { _edgeColor = value; MarkDirtyRepaint(); }
        }

        /// <summary>How fast the colour goes from the centre's to the edge's: 1 is even, above 1 keeps the middle light
        /// for longer and darkens the edges quickly.</summary>
        [UxmlAttribute]
        public float Falloff
        {
            get => _falloff;
            set { _falloff = Mathf.Max(0.1f, value); MarkDirtyRepaint(); }
        }

        /// <summary>Where the light centre is, from 0 (top) to 1 (bottom) of the height. A bit above the middle looks natural.</summary>
        [UxmlAttribute]
        public float CenterY
        {
            get => _centerY;
            set { _centerY = Mathf.Clamp01(value); MarkDirtyRepaint(); }
        }

        public TkBackdrop()
        {
            AddToClassList("kt-backdrop");
            pickingMode = PickingMode.Ignore;
            generateVisualContent += OnGenerateVisualContent;
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            Rect rect = contentRect;
            if (rect.width <= 0f || rect.height <= 0f) return;

            int side = Cells + 1;
            MeshWriteData mesh = context.Allocate(side * side, Cells * Cells * 6);

            for (int y = 0; y < side; y++)
            {
                float v = (float)y / Cells;
                for (int x = 0; x < side; x++)
                {
                    float u = (float)x / Cells;
                    mesh.SetNextVertex(new Vertex
                    {
                        position = new Vector3(rect.xMin + u * rect.width, rect.yMin + v * rect.height, Vertex.nearZ),
                        tint = ColorAt(u, v),
                    });
                }
            }

            for (int y = 0; y < Cells; y++)
            {
                for (int x = 0; x < Cells; x++)
                {
                    ushort a = (ushort)(y * side + x);
                    ushort b = (ushort)(a + 1);
                    ushort c = (ushort)(a + side);
                    ushort d = (ushort)(c + 1);
                    mesh.SetNextIndex(a); mesh.SetNextIndex(b); mesh.SetNextIndex(c);
                    mesh.SetNextIndex(b); mesh.SetNextIndex(d); mesh.SetNextIndex(c);
                }
            }
        }

        // u and v go from 0 to 1 over the width and the height. The distance to the centre is measured in half
        // widths and half heights, so the light is an ellipse that fits the element, and 1 is a corner.
        private Color ColorAt(float u, float v)
        {
            float dx = (u - 0.5f) * 2f;
            float dy = (v - _centerY) * 2f;
            float distance = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) / 1.5f);
            return Color.Lerp(_centerColor, _edgeColor, Mathf.Pow(distance, _falloff));
        }
    }
}
