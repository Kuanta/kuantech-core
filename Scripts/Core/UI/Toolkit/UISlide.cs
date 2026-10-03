namespace Kuantech.Core.UI
{
    /// <summary>How a screen change is shown (see UIToolkitManager.SetRoot).</summary>
    public enum UISlide
    {
        /// <summary>No animation: the new screen is there at once.</summary>
        None,

        /// <summary>The new screen comes in from the right, the old one leaves to the left.</summary>
        FromRight,

        /// <summary>The new screen comes in from the left, the old one leaves to the right.</summary>
        FromLeft,
    }
}
