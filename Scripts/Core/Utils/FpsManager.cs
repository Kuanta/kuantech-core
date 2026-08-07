using TMPro;
using UnityEngine;

namespace Kuantech.Utils
{
    public class FpsManager : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_Text FpsCounter;

        [Header("Frame Settings")]
        [Tooltip("Frame rate cap the platform paces to natively. Set high (e.g. 9999) for uncapped.")]
        public int TargetFrameRate = 60;

        [Header("Display")]
        [Tooltip("0..1 smoothing for the displayed FPS. Higher = snappier, lower = steadier.")]
        [SerializeField] private float FpsSmoothing = 0.1f;

        [Header("Display")]
        [Tooltip("Append the render surface size to the counter, and log it whenever it changes.")]
        [SerializeField] private bool ShowScreenSize;

        private float _smoothedFps;
        private int _lastWidth;
        private int _lastHeight;

        private void Awake()
        {
            // Let the platform pace frames natively — no main-thread sleeping.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;
        }

        private void Update()
        {
            TrackScreenSize();
            if (FpsCounter == null) return;
            // FPS is 1 / the real frame delta (unscaled so a paused/slowed game still reads true), smoothed
            // so the number doesn't flicker every frame.
            float instantFps = Time.unscaledDeltaTime > 0f ? 1.0f / Time.unscaledDeltaTime : 0f;
            _smoothedFps = Mathf.Lerp(_smoothedFps, instantFps, FpsSmoothing);
            FpsCounter.text = ShowScreenSize
                ? $"FPS:{Mathf.RoundToInt(_smoothedFps)}  {Screen.width}x{Screen.height}"
                : $"FPS:{Mathf.RoundToInt(_smoothedFps)}";
        }

        /// <summary>
        /// Reports the render surface every time it changes size. On a device this is the only way to see
        /// whether Unity's surface actually followed the window: a surface left at the portrait size inside
        /// a landscape window is what puts the picture in part of the screen and leaves the rest stale.
        /// </summary>
        private void TrackScreenSize()
        {
            if (!ShowScreenSize) return;
            if (Screen.width == _lastWidth && Screen.height == _lastHeight) return;

            Debug.Log($"[{nameof(FpsManager)}] surface {_lastWidth}x{_lastHeight} -> {Screen.width}x{Screen.height} " +
                      $"(display {Screen.currentResolution.width}x{Screen.currentResolution.height}, " +
                      $"orientation {Screen.orientation}, safeArea {Screen.safeArea})");

            _lastWidth = Screen.width;
            _lastHeight = Screen.height;
        }
    }
}
