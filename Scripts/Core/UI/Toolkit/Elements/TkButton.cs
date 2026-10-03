using UnityEngine.UIElements;

namespace Kuantech.Core.UI
{
    /// <summary>
    /// The button of the menus. In UXML: <c>&lt;Kuantech.Core.UI.TkButton text="Play" /&gt;</c>. The feedback it asks for
    /// on click is an attribute, so it can be changed in UI Builder's Inspector.
    /// </summary>
    [UxmlElement]
    public partial class TkButton : Button
    {
        /// <summary>What to report when the button is clicked. None keeps it silent.</summary>
        [UxmlAttribute]
        public UIFeedbackType ClickFeedback { get; set; } = UIFeedbackType.Click;

        public TkButton()
        {
            AddToClassList("kt-button");
            clicked += OnClicked;
        }

        private void OnClicked() => UIFeedback.Play(ClickFeedback);
    }
}
