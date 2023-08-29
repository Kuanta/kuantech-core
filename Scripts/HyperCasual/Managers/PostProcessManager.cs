using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Kuantech.Core.HyperCasual
{
    public class PostProcessManager : SubManager
    {
        [SerializeField] public Volume PostProcessVolume;

        private IEnumerator vignetteCoroutine = null;
        public void ShowVignette(float duration, float intensity)
        {
            if (vignetteCoroutine != null) return;
            if (!PostProcessVolume.profile.TryGet<Vignette>(out Vignette vignette)) return;
            vignette.intensity.value = intensity;
            vignetteCoroutine = ResetVignetteEffect(vignette, intensity, duration);
            StartCoroutine(vignetteCoroutine);
        }

        private IEnumerator ResetVignetteEffect(Vignette vignette, float intensity, float duration)
        {
            float startTime = Time.time;
            while (Time.time < startTime + duration*0.5f)
            {
                float t = (Time.time - startTime) / (duration*0.5f);
                vignette.intensity.value = Mathf.Lerp(0f, intensity, t);
                yield return null;
            }

            vignette.intensity.value = intensity;
            startTime = Time.time;
            while (Time.time < startTime + duration*0.5f)
            {
                float t = (Time.time - startTime) / (duration*0.5f);
                vignette.intensity.value = Mathf.Lerp(intensity, 0f, t);
                yield return null;
            }

            vignette.intensity.value = 0f;
            vignetteCoroutine = null;
        }
    }
}