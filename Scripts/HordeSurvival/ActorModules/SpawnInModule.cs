using System.Collections;
using Kuantech.Core;
using Kuantech.Core.FX;
using UnityEngine;

namespace Kuantech.HordeSurvival
{
    /// <summary>
    /// Shared spawn-in for any horde agent (grunt or worker): plays a spawn animation + effect and holds
    /// <see cref="IsReady"/> false for a short beat, so a freshly spawned or recycled agent eases in instead
    /// of popping into the fight. The brain gates its movement on <see cref="IsReady"/>; this module owns
    /// only the visual + the delay. Triggered explicitly by the spawner (with the final position) so the
    /// effect lands where the agent actually ends up, not where the pooled body last was.
    /// </summary>
    public class SpawnInModule : ActorModule
    {
        [Tooltip("Spawn-in animation, played on spawn and on relocation.")]
        [SerializeField] private AnimationData SpawnAnimation;
        [SerializeField] private EffectPlayer SpawnEffect;
        [Tooltip("How long the agent stays frozen (playing the spawn-in) before the brain takes over.")]
        [SerializeField] private float BecomeActiveDelay = 0.75f;

        private AnimationModule _animationModule;
        private IEnumerator _routine;

        // True once the spawn-in beat is over. Starts true so an agent with no spawn-in triggered (e.g. a
        // test-scene drop) is immediately controllable.
        public bool IsReady { get; private set; } = true;

        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            _animationModule = Actor.GetModule<AnimationModule>();
        }

        /// <summary>Plays the spawn-in at <paramref name="position"/> and holds IsReady false for the delay.</summary>
        public void Play(Vector3 position)
        {
            if (SpawnAnimation != null && _animationModule != null)
                _animationModule.PlayAnimationData(SpawnAnimation);

            if (SpawnEffect != null)
                SpawnEffect.PlayEffectAtPosition(position, Actor.transform.rotation);

            IsReady = false;
            if (_routine != null) StopCoroutine(_routine);
            _routine = BecomeReadyRoutine();
            StartCoroutine(_routine);
        }

        private IEnumerator BecomeReadyRoutine()
        {
            yield return new WaitForSeconds(BecomeActiveDelay);
            IsReady = true;
        }

        public override void Cleanup()
        {
            base.Cleanup();
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;
            IsReady = true; // pooled reuse starts clean
        }
    }
}
