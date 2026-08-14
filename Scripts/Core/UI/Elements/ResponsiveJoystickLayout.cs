using UnityEngine;

namespace Kuantech.Core.UI
{
    [RequireComponent(typeof(TouchJoystick))]
    [ExecuteAlways]
    public class ResponsiveJoystickLayout : MonoBehaviour
    {
        public enum JoystickSide
        {
            Left,
            Right
        }

        [Header("Layout Settings")]
        [SerializeField] private JoystickSide Side = JoystickSide.Left;
        [Tooltip("What fraction of screen height should the joystick capture zone occupy?")]
        [Range(0.1f, 1.0f)] [SerializeField] private float HeightFraction = 0.8f;

        [Header("Visual Scaling")]
        [Tooltip("Scale of the joystick background in landscape mode.")]
        [SerializeField] private float LandscapeScale = 1.0f;
        [Tooltip("Scale of the joystick background in portrait mode.")]
        [SerializeField] private float PortraitScale = 0.75f;

        [Header("Home Position (Relative to Half-Screen)")]
        [Tooltip("Normalized position of background in landscape (X relative to side edge, Y relative to bottom).")]
        [SerializeField] private Vector2 LandscapeHomeRatio = new Vector2(0.35f, 0.25f);
        [Tooltip("Normalized position of background in portrait (X relative to side edge, Y relative to bottom).")]
        [SerializeField] private Vector2 PortraitHomeRatio = new Vector2(0.4f, 0.2f);

        private TouchJoystick _joystick;
        private RectTransform _rectTransform;
        private RectTransform _backgroundRect;

        private int _lastScreenWidth;
        private int _lastScreenHeight;

        private void Awake()
        {
            InitializeReferences();
            ApplyLayout();
        }

        private void Start()
        {
            ApplyLayout();
        }

        private void InitializeReferences()
        {
            _joystick = GetComponent<TouchJoystick>();
            _rectTransform = GetComponent<RectTransform>();
            
            if (_backgroundRect == null)
            {
                Transform bgTransform = transform.Find("Background");
                if (bgTransform != null)
                {
                    _backgroundRect = bgTransform.GetComponent<RectTransform>();
                }
            }
        }

        private void Update()
        {
            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
            {
                ApplyLayout();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            InitializeReferences();
            ApplyLayout();
        }
#endif

        public void ApplyLayout()
        {
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null) return;

            InitializeReferences();

            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;

            bool isPortrait = _lastScreenHeight > _lastScreenWidth;

            if (Side == JoystickSide.Left)
            {
                _rectTransform.anchorMin = new Vector2(0f, 0f);
                _rectTransform.anchorMax = new Vector2(0.5f, HeightFraction);
                _rectTransform.pivot = new Vector2(0f, 0f);
            }
            else
            {
                _rectTransform.anchorMin = new Vector2(0.5f, 0f);
                _rectTransform.anchorMax = new Vector2(1f, HeightFraction);
                _rectTransform.pivot = new Vector2(1f, 0f);
            }

            _rectTransform.sizeDelta = Vector2.zero;
            _rectTransform.anchoredPosition = Vector2.zero;

            if (_backgroundRect != null)
            {
                float currentScale = isPortrait ? PortraitScale : LandscapeScale;
                _backgroundRect.localScale = new Vector3(currentScale, currentScale, 1f);

                RectTransform parentRect = _rectTransform.parent as RectTransform;
                float containerWidth = 0f;
                float containerHeight = 0f;

                if (parentRect != null)
                {
                    containerWidth = parentRect.rect.width * 0.5f;
                    containerHeight = parentRect.rect.height * HeightFraction;
                }
                else
                {
                    Canvas canvas = GetComponentInParent<Canvas>();
                    if (canvas != null && canvas.transform is RectTransform canvasRect)
                    {
                        containerWidth = canvasRect.rect.width * 0.5f;
                        containerHeight = canvasRect.rect.height * HeightFraction;
                    }
                }

                if (containerWidth > 0 && containerHeight > 0)
                {
                    Vector2 homeRatio = isPortrait ? PortraitHomeRatio : LandscapeHomeRatio;
                    Vector2 calculatedHomePosition;

                    if (Side == JoystickSide.Left)
                    {
                        _backgroundRect.anchorMin = new Vector2(0f, 0f);
                        _backgroundRect.anchorMax = new Vector2(0f, 0f);
                        _backgroundRect.pivot = new Vector2(0.5f, 0.5f);
                        calculatedHomePosition = new Vector2(containerWidth * homeRatio.x, containerHeight * homeRatio.y);
                    }
                    else
                    {
                        _backgroundRect.anchorMin = new Vector2(1f, 0f);
                        _backgroundRect.anchorMax = new Vector2(1f, 0f);
                        _backgroundRect.pivot = new Vector2(0.5f, 0.5f);
                        calculatedHomePosition = new Vector2(-containerWidth * homeRatio.x, containerHeight * homeRatio.y);
                    }

                    _backgroundRect.anchoredPosition = calculatedHomePosition;

                    if (_joystick != null)
                    {
                        _joystick.UpdateHomePosition(calculatedHomePosition);
                    }
                }
            }
        }
    }
}
