// AnimationMontage.cs
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Kuantech.Core
{
    /// <summary>
    /// Data container that describes how to play a single animation "montage-like".
    /// - Which layer to use (by name or index)
    /// - Which state to play (full path)
    /// - Entry fade, start time, playback speed
    /// - Optional layer weight fade-in/out around the montage
    /// </summary>
    [CreateAssetMenu(fileName = "AnimationMontage", menuName = "Kuantech/Animation/AnimationMontage")]
    public class AnimationMontage : ScriptableObject
    {
        [Header("Animation Timing")] 
        [Tooltip("Total duration of the montage")]
        public float MontageDuration = 1.0f;
        
        [Header("Where (Layer)")]
        [Tooltip("Animator layer name (e.g., \"Attack\"). If set, overrides layerIndex.")]
        public string LayerName;

        [Tooltip("Layer index to use if layerName is empty.")]
        public int LayerIndex = 0;

        [Header("What (State)")]
        [Tooltip("Full path of the target state, e.g., \"Base Layer.Attack/Slash\". " +
                 "Using full path is important for Animator.HasState with a fullPathHash.")]
        public string FullPath;

        [Header("How (Crossfade & Time)")]
        [Tooltip("Crossfade duration in seconds. 0 = instant Play()")]
        public float FadeTime = 0.10f;

        [Tooltip("Normalized start time [0..1]. 0 = at the beginning of the clip")]
        [Range(0f, 1f)] public float NormalizedStartTime = 0f;

        [Tooltip("Playback speed multiplier (affects Animator.speed while the montage is active)")]
        public float SpeedMultiplier = 1f;

       [Tooltip("Use fixed-time crossfade (CrossFadeInFixedTime) instead of normalized CrossFade.")]
        public bool UseFixedTime = true;

        [FormerlySerializedAs("restartIfPlaying")] [Tooltip("If the target state is already playing on the layer, restart it from normalizedStartTime.")]
        public bool RestartIfPlaying = true;

        [Header("Layer Weight Blend (UE Montage-like)")]
        [Tooltip("Fade-in time for the layer weight (to 1). 0 = set instantly.")]
        public float LayerFadeIn = 0.08f;

        [Tooltip("Fade-out time for the layer weight (to 0) after the montage finishes. 0 = set instantly.")]
        public float LayerFadeOut = 0.12f;

        [Tooltip("Automatically fade out the layer when the state finishes (normalizedTime >= 1).")]
        public bool AutoFadeOutOnComplete = true;

        [Tooltip("Restore Animator.speed to 1 after auto fade-out completes.")]
        public bool RestoreSpeedOnComplete = true;

        /// <summary>
        /// Resolve the layer index based on layerName or layerIndex.
        /// </summary>
        public int ResolveLayerIndex(Animator animator)
        {
            if (!string.IsNullOrEmpty(LayerName))
            {
                int idx = animator.GetLayerIndex(LayerName);
                return idx < 0 ? 0 : idx;
            }
            return Mathf.Clamp(LayerIndex, 0, Mathf.Max(0, animator.layerCount - 1));
        }

        /// <summary>
        /// Precomputed hash for Animator.HasState / CrossFade / Play with a full path.
        /// </summary>
        public int FullPathHash => Animator.StringToHash(FullPath);
    }
}
