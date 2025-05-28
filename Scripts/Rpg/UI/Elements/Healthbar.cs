using Kuantech.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Rpg
{
    public class Healthbar : MonoBehaviour
    {
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

        public void SetHealth(float current, float max)
        {
            if (HideOnFullHealth && current >= max && HideParent != null)
            {
                FrontBar.value = 1;
                BackBar.value = 1;
                ToggleVisual(false);
                return;
            }
            
            ToggleVisual(true);
            
            current = Mathf.Clamp(current, 0, max);
            
            if (CurrentHealthText != null)
            {
                CurrentHealthText.text = current.Stringfy();
            }

            if (MaxHealthText != null)
            {
                MaxHealthText.text = current.Stringfy();
            }
            
            _targetFill = Mathf.Clamp01(current / max);
            if (FrontBar != null)
                FrontBar.value = _targetFill;

            if (BackBar == null) return;
            
            //If back bar is already smaller, set it instantly
            if (BackBar.value <= _targetFill)
            {
                BackBar.value = _targetFill;
            }
            else
            {
                _backBarDelayTimer = BackBarDelay;
                _backAnimInProgress = true;
            }
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

            // Zamanlayıcıyı çalıştır
            if (_backBarDelayTimer > 0f)
            {
                _backBarDelayTimer -= Time.deltaTime;
                return;
            }

            // Lerp ile arkadaki bar'ı düşür
            float current = BackBar.value;
            current = Mathf.MoveTowards(current, _targetFill, Time.deltaTime * BackBarLerpSpeed);
            BackBar.value = current;

            if (Mathf.Approximately(current, _targetFill))
            {
                _backAnimInProgress = false;
            }
        }

        public void Reset()
        {
            FrontBar.value = 1.0f;
            BackBar.value = 1.0f;
        }

    }
}