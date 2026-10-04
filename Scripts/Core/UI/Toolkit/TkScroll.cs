using UnityEngine.UIElements;

namespace Kuantech.Core.UI
{
    /// <summary>
    /// Makes a ScrollView work like a list on a phone, so every scrolling area of the game feels the same: no scroll bar, a finger
    /// drags it and it goes on and gives at the ends (the ScrollView's own touch scrolling, set to the same numbers), and a mouse
    /// drags it with the same feel (<see cref="TkDragScroll"/>, for the editor). A ListView has a ScrollView inside it:
    /// <c>TkScroll.Setup(listView.Q&lt;ScrollView&gt;())</c>. In UXML use <see cref="TkScrollView"/>, which does this by itself.
    /// </summary>
    public static class TkScroll
    {
        /// <summary>How strongly a finger can pull the content past an end (UI Toolkit's elasticity; 0.1 is its default).</summary>
        public const float Elasticity = 0.1f;

        /// <summary>The share of its speed a list has left after one second (the same as <see cref="ScrollPhysics.DecelerationRate"/>).</summary>
        public const float DecelerationRate = 0.135f;

        public static void Setup(ScrollView scroll)
        {
            scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.touchScrollBehavior = ScrollView.TouchScrollBehavior.Elastic;
            scroll.elasticity = Elasticity;
            scroll.scrollDecelerationRate = DecelerationRate;
            scroll.AddManipulator(new TkDragScroll());
        }
    }

    /// <summary>A ScrollView that is set up with <see cref="TkScroll.Setup"/>. In UXML: <c>&lt;Kuantech.Core.UI.TkScrollView&gt; ... &lt;/Kuantech.Core.UI.TkScrollView&gt;</c></summary>
    [UxmlElement]
    public partial class TkScrollView : ScrollView
    {
        public TkScrollView()
        {
            TkScroll.Setup(this);
        }
    }
}
