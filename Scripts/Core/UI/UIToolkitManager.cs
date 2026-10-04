using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Kuantech.Core.UI
{
    /// <summary>
    /// SubManager that owns the menu UI built with UI Toolkit: one UIDocument, a registry of <see cref="UIPanel"/>s
    /// and the stack of the panels that are open.
    ///
    /// Setup:
    ///   - Put it (or a subclass) on a GameObject with a UIDocument, under the scene's SceneSubManagerContainer.
    ///   - Fill <see cref="Templates"/> with one UXML per panel id.
    ///   - A subclass overrides <see cref="RegisterPanels"/> and calls <see cref="Register"/> for each panel.
    ///
    /// Stack rules (see <see cref="RefreshVisibility"/>): a Screen hides everything under it, a Popup lets the panel
    /// under it stay visible. The first panel pushed is the root; it can not be popped. The back button
    /// (Escape / Android back) asks the top panel to close.
    ///
    /// Usage:
    ///   UIToolkitManager.Push("deck");
    ///   UIToolkitManager.Pop();
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UIToolkitManager : SubManager
    {
        [Serializable]
        public struct PanelTemplate
        {
            public string PanelId;
            public VisualTreeAsset Uxml;
        }

        [Header("Panels")]
        [Tooltip("The UXML of each panel, found by the panel's Id.")]
        [SerializeField] private List<PanelTemplate> Templates = new();

        [Tooltip("Pushed as the root of the stack once the panels are built.")]
        [SerializeField] private string DefaultPanelId;

        [Header("Screen change")]
        [Tooltip("How long a screen takes to slide in and out, in seconds. 0 or less: no slide.")]
        [SerializeField] private float SlideDuration = 0.3f;

        [Header("Safe area")]
        [Tooltip("Editor only: pretend the display is covered by this much on each edge (left, top, right, bottom, in panel units), to see how " +
                 "the menus look on a phone with a notch. Changes apply at once in play mode. Leave at 0 for the real device.")]
        [SerializeField] private Vector4 DebugInsets;

        /// <summary>Raised whenever the panel on top of the stack changes.</summary>
        public event Action<UIPanel> TopChanged;

        /// <summary>Raised when the safe area (the part of the display not covered by a notch or the home bar) changes.</summary>
        public event Action<SafeInsets> SafeAreaChanged;

        /// <summary>How much of each edge of the display is covered, in the panel's units. Screens and the HUD are kept clear of it;
        /// a <see cref="UIPanel.FullBleed"/> panel makes its own room from it.</summary>
        public SafeInsets SafeArea { get; private set; } = SafeInsets.Zero;

        /// <summary>Raised when another screen becomes the root of the stack (see <see cref="SetRoot"/>).</summary>
        public event Action<UIPanel> RootChanged;

        private readonly Dictionary<string, UIPanel> _panelById = new();
        private readonly List<UIPanel> _stack = new();

        private VisualElement _documentRoot;

        // Layers, bottom to top: background, screens, HUD, popups.
        private VisualElement _backgroundLayer;
        private VisualElement _screenLayer;
        private VisualElement _hudLayer;
        private VisualElement _popupLayer;

        // A screen change that is sliding: the screen going out stays on until the slide is over.
        private UIPanel _slideOut;
        private UIPanel _slideIn;
        private IVisualElementScheduledItem _slideTimer;

        public UIPanel Top => _stack.Count > 0 ? _stack[_stack.Count - 1] : null;

        /// <summary>The screen at the bottom of the stack. It is never popped, so once the first one is set it is never null.</summary>
        public UIPanel Root => _stack.Count > 0 ? _stack[0] : null;

        // ── SubManager lifecycle ───────────────────────────────────────────────

        public override async UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);

            _documentRoot = GetComponent<UIDocument>().rootVisualElement;
            _backgroundLayer = CreateLayer("ktui-background", _documentRoot);
            _screenLayer = CreateLayer("ktui-screens", _documentRoot);
            _hudLayer = CreateLayer("ktui-hud", _documentRoot);
            _popupLayer = CreateLayer("ktui-popups", _documentRoot);

            // The display can change shape (a phone turned) and the safe area with it; the panel is measured after its layout.
            _documentRoot.RegisterCallback<GeometryChangedEvent>(_ => UpdateSafeArea());
        }

        // Editing the debug insets in the Inspector in play mode shows at once.
        private void OnValidate()
        {
            if (Application.isPlaying && _documentRoot != null) UpdateSafeArea();
        }

        public override void OnSubmanagersInitialized()
        {
            base.OnSubmanagersInitialized();

            // Panels read other SubManagers while they build, so this waits until every one of them is ready.
            RegisterPanels();

            if (!string.IsNullOrEmpty(DefaultPanelId))
                SetRootInternal(DefaultPanelId);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                Back();
        }

        /// <summary>Override and call <see cref="Register"/> for every panel of the game.</summary>
        protected virtual void RegisterPanels() { }

        /// <summary>Instantiates the panel's UXML into the stack root and builds the panel. Call it from RegisterPanels.</summary>
        protected void Register(UIPanel panel)
        {
            if (_panelById.ContainsKey(panel.Id))
            {
                Debug.LogWarning($"[UIToolkitManager] Duplicate panel id '{panel.Id}' -- first registration wins.");
                return;
            }

            VisualTreeAsset uxml = FindTemplate(panel.Id);
            if (uxml == null)
            {
                Debug.LogError($"[UIToolkitManager] No UXML assigned for panel id '{panel.Id}'.", this);
                return;
            }

            VisualElement root = uxml.Instantiate();
            Fill(root);
            ApplySafeArea(panel, root);
            if (IsAlwaysOn(panel)) root.pickingMode = PickingMode.Ignore;
            GetLayer(panel.Kind).Add(root);
            panel.Build(root);
            _panelById[panel.Id] = panel;

            // A HUD or background panel is not pushed by anyone: it is on from the start.
            if (IsAlwaysOn(panel)) panel.Show();
        }

        // ── Static API ─────────────────────────────────────────────────────────

        /// <summary>Makes a screen the root: every panel on the stack is closed (popups included) and this one is shown.
        /// This is what a tab bar does; it is not a Push, so Back does not walk through the tabs. The slide says how the
        /// change is shown: the new screen slides in from that side and the old one out to the other (see <see cref="UISlide"/>).</summary>
        public static void SetRoot(string panelId, UISlide slide = UISlide.None) => GetContext<UIToolkitManager>()?.SetRootInternal(panelId, slide);
        public static void Push(string panelId) => GetContext<UIToolkitManager>()?.PushInternal(panelId);
        public static void Pop() => GetContext<UIToolkitManager>()?.PopInternal();
        public static void PopToRoot() => GetContext<UIToolkitManager>()?.PopToRootInternal();
        public static void Back() => GetContext<UIToolkitManager>()?.BackInternal();

        public static UIPanel GetPanel(string panelId)
        {
            UIToolkitManager ctx = GetContext<UIToolkitManager>();
            if (ctx == null) return null;
            ctx._panelById.TryGetValue(panelId, out UIPanel panel);
            return panel;
        }

        public static T GetPanel<T>(string panelId) where T : UIPanel => GetPanel(panelId) as T;

        // ── Stack ──────────────────────────────────────────────────────────────

        private void SetRootInternal(string panelId, UISlide slide = UISlide.None)
        {
            if (!_panelById.TryGetValue(panelId, out UIPanel panel))
            {
                Debug.LogWarning($"[UIToolkitManager] Can not make unknown panel '{panelId}' the root.");
                return;
            }
            if (panel.Kind != UIPanelKind.Screen)
            {
                Debug.LogWarning($"[UIToolkitManager] '{panelId}' is not a Screen, only a Screen can be the root.");
                return;
            }
            if (_stack.Count == 1 && _stack[0] == panel) return;

            // A slide that is still going on ends at once, so there is only ever one to look after.
            FinishSlide();

            UIPanel previousTop = Top;
            UIPanel oldRoot = Root;
            bool slides = slide != UISlide.None && SlideDuration > 0f && oldRoot != null && oldRoot != panel;

            foreach (UIPanel open in _stack)
            {
                // The old root is hidden when it has slid out, not now.
                if (open != panel && !(slides && open == oldRoot)) open.Hide();
            }
            _stack.Clear();
            _stack.Add(panel);

            if (slides)
            {
                StartSlide(oldRoot, panel, slide);
                UpdateHud();
            }
            else RefreshVisibility();

            RootChanged?.Invoke(panel);
            if (previousTop != panel) TopChanged?.Invoke(panel);
        }

        // The new screen is put outside the screen on one side without a transition, shown, and a moment later (so that
        // the start position has been drawn) both screens get a transition and are moved: the new one to its place, the
        // old one out to the other side. The old one is hidden at the end (FinishSlide).
        private void StartSlide(UIPanel outgoing, UIPanel incoming, UISlide slide)
        {
            float side = slide == UISlide.FromRight ? 100f : -100f;
            _slideOut = outgoing;
            _slideIn = incoming;

            SetTranslate(incoming.Root, side, 0f);
            incoming.Show();

            _slideTimer = incoming.Root.schedule.Execute(() =>
            {
                SetTranslate(incoming.Root, 0f, SlideDuration);
                SetTranslate(outgoing.Root, -side, SlideDuration);
                _slideTimer = incoming.Root.schedule.Execute(FinishSlide).StartingIn((long)(SlideDuration * 1000f) + 30);
            }).StartingIn(30);
        }

        private void FinishSlide()
        {
            if (_slideOut == null) return;

            _slideTimer?.Pause();
            _slideTimer = null;

            ClearTranslate(_slideIn.Root);
            ClearTranslate(_slideOut.Root);
            _slideOut.Hide();
            _slideOut = null;
            _slideIn = null;
        }

        // Moves the element sideways by a percentage of its own width, with a transition when the duration is above 0.
        private static void SetTranslate(VisualElement element, float percent, float duration)
        {
            if (duration > 0f)
            {
                element.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("translate") };
                element.style.transitionDuration = new List<TimeValue> { new TimeValue(duration, TimeUnit.Second) };
                element.style.transitionTimingFunction = new List<EasingFunction> { new EasingFunction(EasingMode.EaseOut) };
            }
            else
            {
                element.style.transitionProperty = StyleKeyword.Null;
                element.style.transitionDuration = StyleKeyword.Null;
                element.style.transitionTimingFunction = StyleKeyword.Null;
            }
            element.style.translate = new Translate(Length.Percent(percent), 0);
        }

        private static void ClearTranslate(VisualElement element)
        {
            element.style.transitionProperty = StyleKeyword.Null;
            element.style.transitionDuration = StyleKeyword.Null;
            element.style.transitionTimingFunction = StyleKeyword.Null;
            element.style.translate = StyleKeyword.Null;
        }

        private void PushInternal(string panelId)
        {
            if (!_panelById.TryGetValue(panelId, out UIPanel panel))
            {
                Debug.LogWarning($"[UIToolkitManager] Can not push unknown panel '{panelId}'.");
                return;
            }
            if (IsAlwaysOn(panel))
            {
                Debug.LogWarning($"[UIToolkitManager] '{panelId}' is a {panel.Kind} panel, it is not pushed on the stack.");
                return;
            }
            if (Top == panel) return;
            if (_stack.Contains(panel))
            {
                Debug.LogWarning($"[UIToolkitManager] Panel '{panelId}' is already on the stack.");
                return;
            }

            _stack.Add(panel);
            RefreshVisibility();
            UIFeedback.Play(UIFeedbackType.Open);
            TopChanged?.Invoke(Top);
        }

        private void PopInternal()
        {
            // The root of the stack stays; there is nothing to go back to.
            if (_stack.Count <= 1) return;

            UIPanel popped = Top;
            _stack.RemoveAt(_stack.Count - 1);
            popped.Hide();
            RefreshVisibility();
            UIFeedback.Play(UIFeedbackType.Close);
            TopChanged?.Invoke(Top);
        }

        private void PopToRootInternal()
        {
            if (_stack.Count <= 1) return;

            for (int i = _stack.Count - 1; i >= 1; i--)
            {
                _stack[i].Hide();
                _stack.RemoveAt(i);
            }
            RefreshVisibility();
            TopChanged?.Invoke(Top);
        }

        private void BackInternal()
        {
            UIPanel top = Top;
            if (top == null || !top.OnBack()) return;
            PopInternal();
        }

        /// <summary>
        /// Walks the stack from the top down: every panel is shown until a Screen has been shown, everything under
        /// that Screen is hidden. Run after each push and pop, so the rule lives in one place.
        /// </summary>
        private void RefreshVisibility()
        {
            bool covered = false;
            for (int i = _stack.Count - 1; i >= 0; i--)
            {
                UIPanel panel = _stack[i];
                if (covered)
                {
                    panel.Hide();
                    continue;
                }
                panel.Show();
                covered = panel.Kind == UIPanelKind.Screen;
            }
            UpdateHud();
        }

        // The HUD is hidden while a panel that asks for it is on the stack (and shown, so it can be seen).
        private void UpdateHud()
        {
            bool hide = false;
            foreach (UIPanel panel in _stack)
            {
                if (panel.IsVisible && panel.HidesHud) hide = true;
            }
            _hudLayer.style.display = hide ? DisplayStyle.None : DisplayStyle.Flex;
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private VisualTreeAsset FindTemplate(string panelId)
        {
            foreach (PanelTemplate template in Templates)
            {
                if (template.PanelId == panelId) return template.Uxml;
            }
            return null;
        }

        // ── Safe area ──────────────────────────────────────────────────────────

        // Measures what the phone covers and moves the screens and the HUD out of it. Popups (their dark area covers the whole display),
        // the background and the panels that ask to be full bleed are left alone.
        private void UpdateSafeArea()
        {
            Vector2 panelSize = _documentRoot.layout.size;
            if (float.IsNaN(panelSize.x) || panelSize.x <= 0f) return;

            SafeInsets insets = SafeAreaCalculator.Calculate(Screen.safeArea, new Vector2(Screen.width, Screen.height), panelSize);
            if (Application.isEditor)
                insets = insets.Plus(new SafeInsets(DebugInsets.x, DebugInsets.y, DebugInsets.z, DebugInsets.w));
            if (insets.Approximately(SafeArea)) return;

            SafeArea = insets;
            foreach (UIPanel panel in _panelById.Values)
                ApplySafeArea(panel, panel.Root);
            SafeAreaChanged?.Invoke(SafeArea);
        }

        private void ApplySafeArea(UIPanel panel, VisualElement root)
        {
            bool kept = !panel.FullBleed && (panel.Kind == UIPanelKind.Screen || panel.Kind == UIPanelKind.Hud);
            SafeInsets insets = kept ? SafeArea : SafeInsets.Zero;
            root.style.left = insets.Left;
            root.style.top = insets.Top;
            root.style.right = insets.Right;
            root.style.bottom = insets.Bottom;
        }

        private static VisualElement CreateLayer(string layerName, VisualElement parent)
        {
            var layer = new VisualElement { name = layerName, pickingMode = PickingMode.Ignore };
            Fill(layer);
            parent.Add(layer);
            return layer;
        }

        private static bool IsAlwaysOn(UIPanel panel) => panel.Kind == UIPanelKind.Hud || panel.Kind == UIPanelKind.Background;

        private VisualElement GetLayer(UIPanelKind kind) => kind switch
        {
            UIPanelKind.Background => _backgroundLayer,
            UIPanelKind.Hud => _hudLayer,
            UIPanelKind.Popup => _popupLayer,
            _ => _screenLayer,
        };

        private static void Fill(VisualElement element)
        {
            element.style.position = Position.Absolute;
            element.style.left = 0;
            element.style.top = 0;
            element.style.right = 0;
            element.style.bottom = 0;
        }
    }
}
