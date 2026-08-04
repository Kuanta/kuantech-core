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

        private float _smoothedFps;

        private void Awake()
        {
            // Let the platform pace frames natively — no main-thread sleeping.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;
        }

        private void Update()
        {
            if (FpsCounter == null) return;
            // FPS is 1 / the real frame delta (unscaled so a paused/slowed game still reads true), smoothed
            // so the number doesn't flicker every frame.
            float instantFps = Time.unscaledDeltaTime > 0f ? 1.0f / Time.unscaledDeltaTime : 0f;
            _smoothedFps = Mathf.Lerp(_smoothedFps, instantFps, FpsSmoothing);
            FpsCounter.text = $"FPS:{Mathf.RoundToInt(_smoothedFps)}";
        }
    }
}
