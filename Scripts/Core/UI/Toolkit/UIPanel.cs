using UnityEngine.UIElements;

namespace Kuantech.Core.UI
{
    /// <summary>How a panel behaves when it is pushed on the UI stack.</summary>
    public enum UIPanelKind
    {
        /// <summary>Full screen. Pushing it hides the screen below (Play -> Deck).</summary>
        Screen,

        /// <summary>Opens over the panel below, which stays visible (Settings, Daily Prize).</summary>
        Popup,

        /// <summary>Always visible, never on the stack, under every screen (the backdrop). It takes no clicks.</summary>
        Background,

        /// <summary>Always visible, never on the stack (currency bar, settings button). Sits over the screens and
        /// under the popups. Its root should ignore picking so only its own buttons take clicks.</summary>
        Hud,
    }

    /// <summary>
    /// Base class of every UI Toolkit menu panel. It is plain C#: the UIToolkitManager instantiates the panel's UXML
    /// into its single UIDocument and hands the result to <see cref="Build"/>. A panel only knows its own element tree.
    ///
    /// Lifecycle: OnBuild once -> (OnShow ... OnHide) any number of times.
    ///
    /// Showing and hiding set <c>display</c> and toggle the <see cref="ShownClass"/> USS class on the root, so
    /// transitions can be written in USS without touching this class.
    /// </summary>
    public abstract class UIPanel
    {
        /// <summary>USS class that is on the root while the panel is shown.</summary>
        public const string ShownClass = "ktui-shown";

        /// <summary>Unique id used to open the panel through the manager.</summary>
        public abstract string Id { get; }

        public virtual UIPanelKind Kind => UIPanelKind.Screen;

        public VisualElement Root { get; private set; }
        public bool IsVisible { get; private set; }

        /// <summary>Called by the manager once, right after the UXML is instantiated.</summary>
        internal void Build(VisualElement root)
        {
            Root = root;
            Root.style.display = DisplayStyle.None;
            OnBuild(root);
        }

        internal void Show()
        {
            if (IsVisible) return;
            IsVisible = true;
            Root.style.display = DisplayStyle.Flex;
            Root.AddToClassList(ShownClass);
            OnShow();
        }

        internal void Hide()
        {
            if (!IsVisible) return;
            IsVisible = false;
            Root.RemoveFromClassList(ShownClass);
            Root.style.display = DisplayStyle.None;
            OnHide();
        }

        /// <summary>The back button (Escape / Android back) was pressed while this panel is on top.
        /// Return true to let the stack close it, false to swallow the press.</summary>
        public virtual bool OnBack() => true;

        /// <summary>Find and cache elements, bind events. Runs once.</summary>
        protected abstract void OnBuild(VisualElement root);

        /// <summary>Every time the panel becomes visible: refresh the data it shows.</summary>
        protected virtual void OnShow() { }

        /// <summary>Every time the panel is hidden.</summary>
        protected virtual void OnHide() { }
    }
}
