using System.Collections;
using Kuantech.Core.FX;
using UnityEngine;

namespace Kuantech.Core.Utils
{
    /// <summary>
    /// Hit flash for a baked crowd agent — the crowd counterpart of FlashSpriteShaderEffect, with the same
    /// inspector fields and the same behaviour, driven the only way it can be here.
    ///
    /// The sprite version clones the material and animates a property on the clone. That cannot work for a
    /// crowd: one shared material is precisely what collapses every agent into a single draw call, so giving
    /// each agent its own material would trade the entire system for one flashing goblin. The flash instead
    /// rides in <see cref="CrowdInstance.EffectData"/>, the per-agent slot in the same buffer that carries
    /// the animation frame, and the shader reads it through the CrowdGetAgentEffect node.
    ///
    /// Packing, which the shader has to agree with:
    ///
    ///     EffectData.x    flash amount, 0 to MaxFlashAmount
    ///     EffectData.yzw  flash colour, HDR — the buffer is float, so values above 1 survive
    ///
    /// The colour is carried per agent rather than left as a material property so that this component keeps
    /// the sprite version's authoring exactly: the colour stays a field here, and two effects on two agents
    /// can flash differently. That does spend the whole vector on the flash; a second per-agent effect means
    /// widening CrowdAgentData, which costs 16 bytes per agent and nothing else.
    ///
    /// It derives from <see cref="ShaderEffect"/> so the existing discovery and triggering paths — ActorVisual
    /// and EffectsModule — find it like any other effect. The inherited material list simply stays empty,
    /// since a crowd agent has no renderer of its own.
    /// </summary>
    public class CrowdFlashShaderEffect : ShaderEffect
    {
        [Header("Crowd Flash Settings")]
        [Tooltip("Time to reach full flash.")]
        public float FlashInDuration = 0.05f;
        [Tooltip("Time to fade back out.")]
        public float FlashOutDuration = 0.2f;
        public float MaxFlashAmount = 1.0f;
        [ColorUsage(true, true, 0f, 8f, -5.0f, 5.0f)]
        public Color FlashColor = Color.white;

        private CrowdAgentRenderer _agentRenderer;
        private Coroutine _flashRoutine;

        public override void PlayShaderEffect()
        {
            base.PlayShaderEffect();

            _agentRenderer = ResolveAgentRenderer();
            if (_agentRenderer == null) return;

            // Restart rather than ignore a flash that is already running: a second hit during the fade-out
            // should flash again. The sprite version drops it, which reads as unresponsive on a horde that
            // is being hit continuously.
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashRoutine());
        }

        public override void StopShaderEffect()
        {
            base.StopShaderEffect();

            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
                _flashRoutine = null;
            }
            SetFlash(0f);
        }

        public override void Reset()
        {
            base.Reset();
            _flashRoutine = null;
            SetFlash(0f);
        }

        /// <summary>
        /// Finds the agent this effect belongs to, at play time rather than in Awake. Two things move under
        /// it: EffectsModule reparents shader effects onto the actor, and the actor's visual can be swapped
        /// at runtime — so neither the effect's own parent chain nor a cached reference stays valid.
        /// </summary>
        private CrowdAgentRenderer ResolveAgentRenderer()
        {
            Actor actor = GetComponentInParent<Actor>(true);
            ActorVisual visual = actor != null && actor.VisualHandler != null
                ? actor.VisualHandler.GetActorVisual()
                : null;

            if (visual != null)
            {
                CrowdAgentRenderer fromVisual = visual.GetComponentInChildren<CrowdAgentRenderer>(true);
                if (fromVisual != null) return fromVisual;
            }

            // No actor, or a visual without an agent: fall back to the effect's own parents, which covers a
            // standalone agent with the effect sitting on or under it.
            return GetComponentInParent<CrowdAgentRenderer>(true);
        }

        private IEnumerator FlashRoutine()
        {
            float timer = 0f;
            while (timer < FlashInDuration)
            {
                timer += Time.deltaTime;
                SetFlash(Mathf.Lerp(0f, MaxFlashAmount, Mathf.Clamp01(timer / FlashInDuration)));
                yield return null;
            }

            timer = 0f;
            while (timer < FlashOutDuration)
            {
                timer += Time.deltaTime;
                SetFlash(Mathf.Lerp(MaxFlashAmount, 0f, Mathf.Clamp01(timer / FlashOutDuration)));
                yield return null;
            }

            SetFlash(0f);
            _flashRoutine = null;
        }

        private void SetFlash(float amount)
        {
            if (_agentRenderer == null) return;
            _agentRenderer.EffectData = new Vector4(amount, FlashColor.r, FlashColor.g, FlashColor.b);
        }
    }
}
