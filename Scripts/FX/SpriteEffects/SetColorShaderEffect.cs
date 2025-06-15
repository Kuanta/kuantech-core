using System.Collections;
using UnityEngine;

namespace Kuantech.Core.FX
{
    public class SetColorShaderEffect : ShaderEffect
    {
        public string ColorPropertyName;
        public float FadeInDuration;
        public float FadeOutDuration;
        public Color ColorToSet;
        public Color UnsetColor;
        
        private IEnumerator _currentCoroutine;
        public override void PlayShaderEffect()
        {
            base.PlayShaderEffect();
            if (_currentCoroutine != null)
            {
                StopCoroutine(_currentCoroutine);
            }

            _currentCoroutine = FadeInCoroutine();
            StartCoroutine(_currentCoroutine);
        }

        public override void StopShaderEffect()
        {
            base.StopShaderEffect();
            if (_currentCoroutine != null)
            {
                StopCoroutine(_currentCoroutine);
            }

            _currentCoroutine = FadeOutCoroutine();
            StartCoroutine(_currentCoroutine);
        }
        
        private IEnumerator FadeInCoroutine()
        {
            float timer = 0f;

            while (timer < FadeInDuration)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / FadeInDuration);
                Color color = Color.Lerp(UnsetColor, ColorToSet, t);
                SetColorProperty(ColorPropertyName, color);
                yield return null;
            }

            _currentCoroutine = null;
        }

        private IEnumerator FadeOutCoroutine()
        {
            float timer = 0f;

            while (timer < FadeOutDuration)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / FadeInDuration);
                Color color = Color.Lerp(ColorToSet, UnsetColor, t);
                SetColorProperty(ColorPropertyName, color);
                yield return null;
            }
            _currentCoroutine = null;
        }
    }
}