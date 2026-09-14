using System;
using UnityEngine;

namespace Kuantech.Core
{
    /// <summary>
    /// Animation LOD for a horde agent: takes the Animator off Unity's auto-update and drives it manually
    /// at a rate that drops with distance from the player. On-screen culling can't help when a hundred
    /// enemies are all in frame, so distant ones simply animate on fewer frames (choppier, but unnoticeable
    /// far away) while nearby ones stay full-rate. Updates are staggered per agent so the herd doesn't all
    /// tick on the same frame (which would just move the spike, not remove it).
    ///
    /// The Animator lives on the ActorVisual (created with the actor, swappable at runtime), so we read it
    /// from <see cref="AnimationModule"/> — which re-resolves it on a visual swap — every frame rather than
    /// caching one reference. Whatever is current is set to AlwaysAnimate (manual Update needs it, otherwise
    /// a culled animator freezes), taken off auto-update, and driven by us. Ownership is always released
    /// (re-enabled) when it changes or on cleanup, so a pooled/reused body is never left frozen.
    /// </summary>
    public class AnimationLodModule : ActorModule
    {
        [Tooltip("Within this distance from the player the animator updates every frame (full rate).")]
        [SerializeField] private float NearDistance = 15f;
        [Tooltip("Between Near and this distance, update every MidInterval-th frame.")]
        [SerializeField] private float MidDistance = 30f;
        [SerializeField] private int MidInterval = 2;
        [Tooltip("Beyond MidDistance, update every FarInterval-th frame.")]
        [SerializeField] private int FarInterval = 4;

        /// <summary>
        /// Cheap shared player position (set once by the run handler) so a hundred agents don't each do
        /// their own lookup. Null (e.g. a test scene) means no LOD source, so everyone animates full-rate.
        /// </summary>
        public static Func<Vector3> PlayerPositionProvider;

        private AnimationModule _animationModule;
        private Animator _animator;
        private float _accumulated;
        private int _stagger;
        private static int _staggerSeed;

        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            _animationModule = Actor.GetModule<AnimationModule>();
            _stagger = _staggerSeed++; // spread agents across frames
        }

        public override void ModuleUpdate(float deltaTime)
        {
            Animator current = _animationModule != null ? _animationModule.Animator : null;

            // Ownership changed (visual swapped, or lost): release the old one (re-enable, so it is never left
            // disabled-and-undriven → a frozen body), then take the new one.
            if (current != _animator)
            {
                Release();
                _animator = current;
                if (_animator != null)
                    // Manual Update needs AlwaysAnimate — with any culling mode a culled animator won't apply
                    // Update() and freezes. This is why we drive culling ourselves instead of the editor.
                    _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
            if (_animator == null) return;
            if (_animator.enabled) _animator.enabled = false; // we drive it via Animator.Update

            _accumulated += deltaTime;

            int interval = GetInterval();
            // Off-frames for this agent: skip, but keep accumulating so the eventual update advances by the
            // real elapsed time (correct speed, just fewer samples).
            if (interval > 1 && (Time.frameCount + _stagger) % interval != 0) return;

            _animator.Update(_accumulated);
            _accumulated = 0f;
        }

        private int GetInterval()
        {
            if (PlayerPositionProvider == null) return 1; // no LOD reference → full rate
            float sqr = (Actor.GetActorLocation() - PlayerPositionProvider()).sqrMagnitude;
            if (sqr <= NearDistance * NearDistance) return 1;
            if (sqr <= MidDistance * MidDistance) return Mathf.Max(1, MidInterval);
            return Mathf.Max(1, FarInterval);
        }

        // Hand the animator back to Unity's auto-update so it is never stranded disabled (pooled reuse, teardown).
        private void Release()
        {
            if (_animator != null) _animator.enabled = true;
            _accumulated = 0f;
        }

        public override void Cleanup()
        {
            base.Cleanup();
            Release();
            _animator = null;
        }
    }
}
