using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Kuantech.Core
{
    /// <summary>
    /// Plays AnimationMontage on an Animator:
    /// - Crossfades directly to a specific state (no params needed)
    /// - Fades the target layer weight in/out to overlay on top of locomotion
    /// - Optionally restores Animator.speed afterward
    ///
    /// Tips:
    /// - Place your attack states on a dedicated "Attack" layer with an AvatarMask for upper body.
    /// - Fill montage.fullPath with the *full* state path string (e.g., "Base Layer.Attack/Slash").
    /// </summary>
    public class AnimationMontagePlayer : MonoBehaviour
    {
        [FormerlySerializedAs("animator")] [Header("References")]
        public Animator Animator;

        [Header("Defaults")]
        [Tooltip("If the layer has no weight yet, this is the target weight for the montage (usually 1).")]
        [Range(0f, 1f)] public float defaultLayerWeight = 1f;

        // Track running coroutines per layer, to avoid overlapping fades fighting each other
        private readonly Dictionary<int, Coroutine> _layerFadeCoroutines = new();
        private readonly Dictionary<int, Coroutine> _autoFadeOutCoroutines = new();

        /// <summary>
        /// Play the given montage: CrossFade (or Play) into the target state on the specified layer,
        /// fade that layer's weight in, and (optionally) auto-fade-out when the state finishes.
        /// </summary>
        public void PlayMontage(AnimationMontage montage)
        {
            if (!Animator || montage == null) return;

            int layer = montage.ResolveLayerIndex(Animator);
            int hash  = montage.FullPathHash;

            // State existence check (important when using fullPath)
            if (!Animator.HasState(layer, hash))
            {
                Debug.LogWarning($"[Montage] State not found: '{montage.FullPath}' (layer {layer}) on {Animator.name}");
                return;
            }

            // If the same state is already playing and restart is disabled, do nothing
            var current = Animator.GetCurrentAnimatorStateInfo(layer);
            bool sameStatePlaying = current.fullPathHash == hash && current.normalizedTime < 1f;
            if (sameStatePlaying && !montage.RestartIfPlaying)
                return;

            // Enter the state
            float t0 = Mathf.Clamp01(montage.NormalizedStartTime);
            if (montage.FadeTime <= 0f)
            {
                Animator.Play(hash, layer, t0);
                return;
            }
            else
            {
                if (montage.UseFixedTime)
                    Animator.CrossFadeInFixedTime(hash, montage.FadeTime, layer, t0);
                else
                    Animator.CrossFade(hash, montage.FadeTime, layer, t0);
            }

            // Fade-in the layer weight to defaultLayerWeight
            StartOrReplaceLayerFade(layer, target: defaultLayerWeight, duration: montage.LayerFadeIn);

            // Auto-fade-out when finished
            if (montage.AutoFadeOutOnComplete)
            {
                StartOrReplaceAutoFadeOut(layer, hash, montage.LayerFadeOut, montage.RestoreSpeedOnComplete);
            }
        }

        /// <summary>
        /// Manually fade the given layer to a weight over duration (seconds).
        /// Any ongoing fade on that layer will be canceled and replaced.
        /// </summary>
        public void FadeLayerWeight(int layer, float target, float duration)
        {
            StartOrReplaceLayerFade(layer, target, duration);
        }

        /// <summary>
        /// Immediately stop any auto-fade-out watcher for the given layer.
        /// </summary>
        public void CancelAutoFadeOut(int layer)
        {
            if (_autoFadeOutCoroutines.TryGetValue(layer, out var c) && c != null)
            {
                StopCoroutine(c);
            }
            _autoFadeOutCoroutines.Remove(layer);
        }

        // ---------------------- Internals ----------------------

        private void StartOrReplaceLayerFade(int layer, float target, float duration)
        {
            if (_layerFadeCoroutines.TryGetValue(layer, out var c) && c != null)
            {
                StopCoroutine(c);
            }
            _layerFadeCoroutines[layer] = StartCoroutine(FadeLayerWeightRoutine(layer, target, duration));
        }

        private IEnumerator FadeLayerWeightRoutine(int layer, float target, float duration)
        {
            target = Mathf.Clamp01(target);

            if (duration <= 0f)
            {
                Animator.SetLayerWeight(layer, target);
                yield break;
            }

            float start = Animator.GetLayerWeight(layer);
            float t = 0f;

            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                Animator.SetLayerWeight(layer, Mathf.Lerp(start, target, k));
                yield return null;
            }

            Animator.SetLayerWeight(layer, target);
        }

        private void StartOrReplaceAutoFadeOut(int layer, int stateHash, float fadeOutDuration, bool restoreSpeed)
        {
            // Cancel any previous watcher
            if (_autoFadeOutCoroutines.TryGetValue(layer, out var c) && c != null)
            {
                StopCoroutine(c);
            }
            _autoFadeOutCoroutines[layer] = StartCoroutine(AutoFadeOutWhenFinishedRoutine(layer, stateHash, fadeOutDuration, restoreSpeed));
        }

        private IEnumerator AutoFadeOutWhenFinishedRoutine(int layer, int stateHash, float fadeOutDuration, bool restoreSpeed)
        {
            // Wait until the target state actually becomes current
            while (true)
            {
                var info = Animator.GetCurrentAnimatorStateInfo(layer);
                if (info.fullPathHash == stateHash) break;
                yield return null;
            }

            // Wait until the state is done (normalizedTime >= 1) or replaced
            while (true)
            {
                var info = Animator.GetCurrentAnimatorStateInfo(layer);
                if (info.fullPathHash != stateHash || info.normalizedTime >= 1f) break;
                yield return null;
            }

            // Fade out the layer to zero
            yield return FadeLayerWeightRoutine(layer, 0f, fadeOutDuration);

            if (restoreSpeed)
                Animator.speed = 1f;

            _autoFadeOutCoroutines.Remove(layer);
        }
    }
}
