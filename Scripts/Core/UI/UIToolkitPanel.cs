using UnityEngine;
using UnityEngine.UIElements;

namespace Kuantech.Core.UI
{
    /// <summary>
    /// Base class for all UI Toolkit panels managed by UIToolkitManager.
    ///
    /// Each panel lives inside a UIDocument.  The panel owns a named root VisualElement
    /// inside that document's hierarchy (PanelRootName).  UIToolkitManager discovers and
    /// registers panels on the scene, then calls Initialize() once after all SubManagers
    /// are ready.  Nothing initializes itself in Awake/Start.
    ///
    /// Subclass pattern:
    ///   public class InventoryPanel : UIToolkitPanel
    ///   {
    ///       public override string PanelId => "inventory";
    ///       protected override void OnInitialize(VisualElement root) { /* query elements */ }
    ///       protected override void OnOpen()  { /* refresh data */ }
    ///       protected override void OnClose() { /* cleanup  */ }
    ///   }
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public abstract class UIToolkitPanel : MonoBehaviour
    {
        /// <summary>Unique string used to look up this panel via UIToolkitManager.GetPanel().</summary>
        public abstract string PanelId { get; }

        /// <summary>
        /// Name of the root VisualElement inside the UXML that wraps this panel's content.
        /// Override if your UXML root differs from the PanelId naming convention.
        /// </summary>
        protected virtual string PanelRootName => PanelId + "-overlay";

        /// <summary>
        /// When true, opening this panel causes UIToolkitManager to unlock and show the cursor.
        /// Modals and menu windows should keep this true. HUDs and always-visible overlays should return false.
        /// </summary>
        public virtual bool TrapsCursor => true;

        /// <summary>
        /// When true, this panel is opened immediately upon initialization.
        /// Ideal for always-visible HUD elements.
        /// </summary>
        public virtual bool AutoOpenOnStart => false;

        public bool IsOpen { get; private set; }

        protected VisualElement PanelRoot { get; private set; }
        private UIDocument _document;
        private bool _initialized;

        protected virtual void Start()
        {
            // If UIToolkitManager hasn't initialized us (e.g. standalone test or outside manager container),
            // initialize ourselves so the panel is never left non-functional.
            if (!_initialized)
            {
                Initialize();
            }
        }

        // ── UIToolkitManager calls this once, after all SubManagers are ready ──

        internal void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            _document = GetComponent<UIDocument>();
            if (_document == null)
            {
                Debug.LogError($"[UIToolkitPanel '{PanelId}'] No UIDocument on this GameObject.");
                return;
            }

            VisualElement root = _document.rootVisualElement;
            PanelRoot = string.IsNullOrEmpty(PanelRootName) ? root : root.Q<VisualElement>(PanelRootName);

            if (PanelRoot == null)
            {
                Debug.LogError($"[UIToolkitPanel '{PanelId}'] Could not find root element '{PanelRootName}' in UXML.");
                PanelRoot = root; // fallback: use document root
            }

            if (AutoOpenOnStart)
            {
                PanelRoot.style.display = DisplayStyle.Flex;
                IsOpen = true;
            }
            else
            {
                PanelRoot.style.display = DisplayStyle.None;
            }

            OnInitialize(PanelRoot);

            if (AutoOpenOnStart)
            {
                OnOpen();
            }
        }

        // ── Public open/close called by UIToolkitManager ───────────────────────

        public void Open()
        {
            if (!_initialized) Initialize();
            if (IsOpen) return;
            IsOpen = true;
            PanelRoot.style.display = DisplayStyle.Flex;
            OnOpen();
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            PanelRoot.style.display = DisplayStyle.None;
            OnClose();
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        // ── Subclass overrides ─────────────────────────────────────────────────

        /// <summary>Called once after the UIDocument is ready. Query and cache your VisualElements here.</summary>
        protected abstract void OnInitialize(VisualElement root);

        /// <summary>Called each time the panel is opened. Refresh data here.</summary>
        protected virtual void OnOpen() { }

        /// <summary>Called each time the panel is closed.</summary>
        protected virtual void OnClose() { }
    }
}
