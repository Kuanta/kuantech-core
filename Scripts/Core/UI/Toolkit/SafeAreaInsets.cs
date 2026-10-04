using UnityEngine;

namespace Kuantech.Core.UI
{
    /// <summary>How much of each edge of the display a phone covers (a notch, the island, the rounded corners, the home bar), in the
    /// panel's units.</summary>
    public readonly struct SafeInsets
    {
        public static readonly SafeInsets Zero = new SafeInsets(0f, 0f, 0f, 0f);

        public SafeInsets(float left, float top, float right, float bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public float Left { get; }
        public float Top { get; }
        public float Right { get; }
        public float Bottom { get; }

        public SafeInsets Plus(SafeInsets other) => new SafeInsets(Left + other.Left, Top + other.Top, Right + other.Right, Bottom + other.Bottom);

        public bool Approximately(SafeInsets other)
        {
            return Mathf.Approximately(Left, other.Left) && Mathf.Approximately(Top, other.Top)
                && Mathf.Approximately(Right, other.Right) && Mathf.Approximately(Bottom, other.Bottom);
        }
    }

    /// <summary>Works out the safe area's insets in the panel's units from what the screen says (pixels).</summary>
    public static class SafeAreaCalculator
    {
        /// <summary>
        /// The safe area is a rectangle in pixels with its origin at the bottom left (Screen.safeArea). The panel is the size of the same
        /// display in the panel's units (they differ by the panel's scale). Each inset is how far the safe area is from that edge.
        /// </summary>
        public static SafeInsets Calculate(Rect safeArea, Vector2 screenSize, Vector2 panelSize)
        {
            if (screenSize.x <= 0f || screenSize.y <= 0f || panelSize.x <= 0f || panelSize.y <= 0f) return SafeInsets.Zero;

            float scaleX = panelSize.x / screenSize.x;
            float scaleY = panelSize.y / screenSize.y;
            return new SafeInsets(
                Mathf.Max(0f, safeArea.xMin) * scaleX,
                Mathf.Max(0f, screenSize.y - safeArea.yMax) * scaleY,
                Mathf.Max(0f, screenSize.x - safeArea.xMax) * scaleX,
                Mathf.Max(0f, safeArea.yMin) * scaleY);
        }
    }
}
