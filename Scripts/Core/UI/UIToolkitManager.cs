using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.UI
{
    /// <summary>
    /// SubManager that owns all UI Toolkit panels in a scene.
    ///
    /// Setup:
    ///   - Add UIToolkitManager as a SubManager on the scene's SceneSubManagerContainer.
    ///   - Assign panels in the Inspector, OR let it auto-discover via GetComponentsInChildren.
    ///   - UIToolkitManager initializes every panel once OnSubmanagersInitialized() fires,
    ///     ensuring SubManagers (ProfileManager, ItemsLibrary, etc.) are all ready first.
    ///
    /// Usage:
    ///   UIToolkitManager.GetPanel("inventory")?.Toggle();
    ///   UIToolkitManager.OpenPanel("inventory");
    ///   UIToolkitManager.ClosePanel("inventory");
    ///   UIToolkitManager.CloseAll();
    /// </summary>
    public class UIToolkitManager : SubManager
    {
        [Header("Panels")]
        [Tooltip("Manually assigned panels. UIToolkitManager also auto-discovers any UIToolkitPanel " +
                 "found in children, so this list is optional.")]
        [SerializeField] private List<UIToolkitPanel> Panels = new();

        [Header("Cursor")]
        [Tooltip("When true, opening any panel unlocks and shows the cursor. " +
                 "Restores the previous lock state when all panels close.")]
        [SerializeField] private bool ManageCursor = true;

        private readonly Dictionary<string, UIToolkitPanel> _panelById = new();
        private int _openPanelCount;
        private CursorLockMode _savedLockMode;
        private bool _savedCursorVisible;

        // ── SubManager lifecycle ───────────────────────────────────────────────

        public override async UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);

            // Register manually assigned panels
            foreach (var panel in Panels)
            {
                if (panel == null) continue;
                RegisterPanel(panel);
            }

            // Auto-discover panels in children not already registered
            foreach (var panel in GetComponentsInChildren<UIToolkitPanel>(includeInactive: true))
            {
                if (panel == null || _panelById.ContainsKey(panel.PanelId)) continue;
                RegisterPanel(panel);
            }
        }

        public override void OnSubmanagersInitialized()
        {
            base.OnSubmanagersInitialized();

            // Initialize all panels now that every SubManager is ready
            foreach (var panel in _panelById.Values)
            {
                panel.Initialize();
            }
        }

        /// <summary>Returns the panel registered under panelId, or null.</summary>
        public static UIToolkitPanel GetPanel(string panelId)
        {
            var ctx = GetContext<UIToolkitManager>();
            if (ctx == null) return null;
            ctx._panelById.TryGetValue(panelId, out var panel);
            return panel;
        }

        public static T GetPanel<T>(string panelId) where T : UIToolkitPanel
            => GetPanel(panelId) as T;

        public static void OpenPanel(string panelId)
        {
            var ctx = GetContext<UIToolkitManager>();
            var panel = ctx?.GetPanelInternal(panelId);
            if (panel == null || panel.IsOpen) return;
            panel.Open();
            if (panel.TrapsCursor) ctx.OnPanelOpened();
        }

        public static void ClosePanel(string panelId)
        {
            var ctx = GetContext<UIToolkitManager>();
            var panel = ctx?.GetPanelInternal(panelId);
            if (panel == null || !panel.IsOpen) return;
            panel.Close();
            if (panel.TrapsCursor) ctx.OnPanelClosed();
        }

        public static void TogglePanel(string panelId)
        {
            var ctx = GetContext<UIToolkitManager>();
            var panel = ctx?.GetPanelInternal(panelId);
            if (panel == null) return;
            if (panel.IsOpen)
            {
                panel.Close();
                if (panel.TrapsCursor) ctx.OnPanelClosed();
            }
            else
            {
                panel.Open();
                if (panel.TrapsCursor) ctx.OnPanelOpened();
            }
        }

        /// <summary>Closes every currently open modal panel and restores cursor state.</summary>
        public static void CloseAll()
        {
            var ctx = GetContext<UIToolkitManager>();
            if (ctx == null) return;
            foreach (var panel in ctx._panelById.Values)
            {
                // Leave always-visible HUD elements open
                if (!panel.IsOpen || panel.AutoOpenOnStart) continue;
                panel.Close();
                if (panel.TrapsCursor)
                    ctx._openPanelCount = Mathf.Max(0, ctx._openPanelCount - 1);
            }
            if (ctx._openPanelCount <= 0)
            {
                ctx._openPanelCount = 0;
                ctx.RestoreCursor();
            }
        }

        // ── Cursor management ──────────────────────────────────────────────────

        private void OnPanelOpened()
        {
            _openPanelCount++;
            if (ManageCursor && _openPanelCount == 1)
                FreeCursor();
        }

        private void OnPanelClosed()
        {
            _openPanelCount = Mathf.Max(0, _openPanelCount - 1);
            if (ManageCursor && _openPanelCount == 0)
                RestoreCursor();
        }

        private void FreeCursor()
        {
            _savedLockMode      = Cursor.lockState;
            _savedCursorVisible = Cursor.visible;
            Cursor.lockState    = CursorLockMode.None;
            Cursor.visible      = true;
        }

        private void RestoreCursor()
        {
            Cursor.lockState = _savedLockMode;
            Cursor.visible   = _savedCursorVisible;
        }

        // ── Instance helpers ───────────────────────────────────────────────────

        private UIToolkitPanel GetPanelInternal(string panelId)
        {
            _panelById.TryGetValue(panelId, out var panel);
            return panel;
        }

        private void RegisterPanel(UIToolkitPanel panel)
        {
            string id = panel.PanelId;
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"[UIToolkitManager] Panel on '{panel.gameObject.name}' has an empty PanelId -- skipped.");
                return;
            }
            if (_panelById.ContainsKey(id))
            {
                Debug.LogWarning($"[UIToolkitManager] Duplicate PanelId '{id}' on '{panel.gameObject.name}' -- first registration wins.");
                return;
            }
            _panelById[id] = panel;
        }
    }
}
