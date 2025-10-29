using System.Collections;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.FX
{
    public class FlashSpriteShaderEffect : ShaderEffect
    {
        [Header("Sprite Flash Settings")]
        public string FlashProperty = "_FlashAmount";
        public float FlashInDuration = 0.05f;
        public string FlashColorProperty = "_FlashColor";
        [ColorUsageAttribute(true,true,0f,8f,-5.0f,5.0f)]
        public Color FlashColor = Color.white;
        public float FlashOutDuration = 0.2f;
        public float MaxFlashAmount = 1.0f;


        private IEnumerator _flashCoroutine;
        public override void PlayShaderEffect()
        {
            base.PlayShaderEffect();
            if (_flashCoroutine != null) return;
            _flashCoroutine = FlashCoroutine();
            StartCoroutine(_flashCoroutine);
        }

        private IEnumerator FlashCoroutine()
        {
            float timer = 0f;
            while (timer < FlashInDuration)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / FlashInDuration);
                SetFlashAmount(Mathf.Lerp(0f, MaxFlashAmount, t));
                yield return null;
            }

            // Geri düşüş (yavaş azalma)
            timer = 0f;
            while (timer < FlashOutDuration)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / FlashOutDuration);
                SetFlashAmount(Mathf.Lerp(MaxFlashAmount, 0f, t));
                yield return null;
            }

            SetFlashAmount(0f);
            _flashCoroutine = null;
        }

        private void SetFlashAmount(float value)
        {
            if (MaterialInstances.IsNullOrEmpty()) return;
            foreach (var mat in MaterialInstances)
            {
                if (mat == null) return;
                SetFlashColor(mat);
                if (mat.HasProperty(FlashProperty))
                    mat.SetFloat(FlashProperty, value);
            }
        }

        private void SetFlashColor(Material matInstance)
        {
            if (matInstance == null) return;
            matInstance.SetColor(FlashColorProperty, FlashColor);
        }
    }
}