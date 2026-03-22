using Kuantech.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Rpg
{
    public class ResourceBar : MonoBehaviour
    {
        public ResourceAsset ResourceAsset;
        [Header("Settings")]
        public bool ShowAlways = false;
        
        [Header("Bar Images")]
        public Slider FrontBar;  // Anında güncellenen bar
        public Slider BackBar;   // Yavaşça güncellenen bar (arkada kalan)

        [Header("Texts")] 
        public TMP_Text CurrentHealthText;
        public TMP_Text MaxHealthText;
        
        [Header("Animation Settings")]
        public float BackBarDelay = 0.4f;    // Kaç saniye sonra animasyon başlasın
        public float BackBarLerpSpeed = 2f;  // Lerp hızı

        public bool HideOnFullHealth = true;
        public GameObject HideParent;
        
        private float _targetFill;
        private float _currentBackFill;
        private float _backBarDelayTimer;

        private bool _backAnimInProgress;

        private float _lastTargetFill = 1f;

        public void SetHealth(float current, float max)
        {
            current = Mathf.Clamp(current, 0, max);

            if (CurrentHealthText) CurrentHealthText.text = current.Stringfy();
            if (MaxHealthText)     MaxHealthText.text     = max.Stringfy(); // <-- BUG düzeltme: current değil max

            if (!FrontBar || !BackBar) return;

            var newTarget = max > 0 ? Mathf.Clamp01(current / max) : 0f;

            // Full ise gizleme
            if (HideOnFullHealth && newTarget >= 1f && HideParent)
            {
                FrontBar.value = 1f;
                BackBar.value  = 1f;
                _backAnimInProgress = false;
                _backBarDelayTimer  = 0f;
                ToggleVisual(ShowAlways);
                _targetFill     = 1f;
                _lastTargetFill = 1f;
                return;
            }

            ToggleVisual(true);

            // Ön bar anında gerçek sağlığa gider
            FrontBar.value = newTarget;

            bool isHeal = newTarget > _lastTargetFill + Mathf.Epsilon;

            if (isHeal)
            {
                // HEAL: trailing bar'ı anında yeni hedefe çek, animasyonu iptal et
                BackBar.value        = newTarget;
                _backAnimInProgress  = false;
                _backBarDelayTimer   = 0f;
            }
            else
            {
                // DAMAGE: gecikmeli düşür
                if (BackBar.value <= newTarget)
                {
                    // edge-case güvenlik
                    BackBar.value       = newTarget;
                    _backAnimInProgress = false;
                    _backBarDelayTimer  = 0f;
                }
                else
                {
                    _targetFill          = newTarget;
                    _backBarDelayTimer   = BackBarDelay;
                    _backAnimInProgress  = true;
                }
            }

            _targetFill     = newTarget;
            _lastTargetFill = newTarget;
        }

        public void ToggleVisual(bool toggle)
        {
            if (HideParent == null) return;
            HideParent.SetActive(toggle);
        }
        [Sirenix.OdinInspector.Button("Test Fill")]
        private void TestFill(float fill)
        {
            if (FrontBar != null)
                FrontBar.value = _targetFill;
        }
        
        private void Update()
        {
            if (!_backAnimInProgress || BackBar == null) return;

            if (_backBarDelayTimer > 0f)
            {
                _backBarDelayTimer -= Time.deltaTime;
                return;
            }

            float v = Mathf.MoveTowards(BackBar.value, _targetFill, Time.deltaTime * BackBarLerpSpeed);
            BackBar.value = v;

            if (Mathf.Approximately(v, _targetFill))
                _backAnimInProgress = false;
        }

        public void Reset()
        {
            FrontBar.value = 1.0f;
            BackBar.value = 1.0f;
        }

    }
}