using System.Collections;
using Kuantech.Core.FX;
using Kuantech.PostProcessing;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Kuantech.Core
{
    public class ChromaticAbberationEffect : FxBehaviour
    {
        [Header("Timing")]
        public float Duration = 0.5f; // total in+out
        [Tooltip("Kavis (0=linear, 2~3= daha yumuşak)")]
        [Range(0f, 4f)] public float EasePower = 2f;

        [Header("Targets")]
        [Tooltip("0..1 arası (URP)")]
        [Range(0f, 1f)] public float TargetChromaticAbberation = 0.2f;

        [Tooltip("-1..1 arası (URP) | tipik 0.15-0.3")]
        [Range(-1f, 1f)] public float TargetLensDistortion = 0.2f;

        // Runtime
        private Coroutine _routine;
        private Volume _volume;
        private VolumeProfile _profile;

        private ChromaticAberration _ca;
        private LensDistortion _ld;

        private bool _caHadOverride;
        private bool _ldHadOverride;

        private float _origCA;
        private float _origLD;

        protected override void OnFxStarted(Effect parentFx)
        {
            base.OnFxStarted(parentFx);

            _volume = PostProcessingManager.GetMainVolume();
            if (_volume == null)
            {
                Debug.LogWarning("[ChromaticAbberationEffect] Main Volume bulunamadı.");
                return;
            }

            _profile = _volume.sharedProfile != null ? _volume.sharedProfile : _volume.profile;
            if (_profile == null)
            {
                Debug.LogWarning("[ChromaticAbberationEffect] Volume Profile yok.");
                return;
            }

            // Override’ları al/ekle
            GetOrAddOverride(ref _ca);
            GetOrAddOverride(ref _ld);

            // Orijinalleri sakla
            _origCA = _ca.intensity.value;
            _origLD = _ld.intensity.value;

            _caHadOverride = _ca.intensity.overrideState;
            _ldHadOverride = _ld.intensity.overrideState;

            // Override state açık olsun ki değerleri biz sürüp anında uygulayalım
            _ca.active = true; _ca.intensity.overrideState = true;
            _ld.active = true; _ld.intensity.overrideState = true;

            // Varsa eski bir rutin, kapat
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(RunEffect());
        }

        public override void OnFxEnded()
        {
            // Erken bitir: her şeyi geri al
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
            Restore();
        }

        private IEnumerator RunEffect()
        {
            // toplam sürenin yarısı in, yarısı out
            float half = Mathf.Max(0.0001f, Duration * 0.5f);

            // IN: orig -> target
            float t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / half);
                float eased = EaseInOut(k, EasePower);

                _ca.intensity.value = Mathf.LerpUnclamped(_origCA, TargetChromaticAbberation, eased);
                _ld.intensity.value = Mathf.LerpUnclamped(_origLD, TargetLensDistortion, eased);

                yield return null;
            }

            // OUT: target -> orig
            t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / half);
                float eased = EaseInOut(k, EasePower);

                _ca.intensity.value = Mathf.LerpUnclamped(TargetChromaticAbberation, _origCA, eased);
                _ld.intensity.value = Mathf.LerpUnclamped(TargetLensDistortion, _origLD, eased);

                yield return null;
            }

            // bitti — temizle
            Restore();
            _routine = null;
        }

        private void Restore()
        {
            if (_ca != null)
            {
                _ca.intensity.value = _origCA;
                _ca.intensity.overrideState = _caHadOverride;
            }

            if (_ld != null)
            {
                _ld.intensity.value = _origLD;
                _ld.intensity.overrideState = _ldHadOverride;
            }
        }

        private void GetOrAddOverride<T>(ref T volumeComponent) where T : VolumeComponent, new()
        {
            if (!_profile.TryGet(out volumeComponent))
            {
                volumeComponent = _profile.Add<T>(true);
            }
        }

        // yumuşak kavis (0=linear, 2=EaseInOutQuad benzeri)
        private static float EaseInOut(float x, float pow)
        {
            x = Mathf.Clamp01(x);
            if (pow <= 0f) return x;
            // hermite tarzı S-curve’i güçle kuvvetlendir
            float s = x * x * (3f - 2f * x); // smoothstep
            return Mathf.Lerp(x, s, Mathf.Clamp01(pow / 3f));
        }
    }
}
