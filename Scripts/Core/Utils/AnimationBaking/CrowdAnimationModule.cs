using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core.Utils
{
    /// <summary>
    /// Drives a baked crowd agent from the same signals an Animator would receive. It registers itself as
    /// <see cref="AnimationModule.Driver"/>, so combat, spawn and gameplay code keep calling AnimationModule
    /// exactly as they do for an ordinary actor and never learn that this one has no Animator.
    ///
    /// What it replaces is the animator controller itself — the state machine is gone, so the rules that used
    /// to live in transitions are declared here instead, as three kinds of binding resolved in a fixed order:
    ///
    ///   1. States   — a bool parameter that holds a clip for as long as it is true (Dead, Yeeted). The
    ///                 highest priority active one wins, and states outrank everything: being dead should
    ///                 cut an attack short, not queue behind it.
    ///   2. Triggers — a one-shot clip that plays to its end and then hands control back (Attack).
    ///   3. Blend    — the resting behaviour, a float blending two clips (locomotion idle to run).
    ///
    /// That covers what an animator controller for a horde agent actually does. Anything richer — layers,
    /// additive poses, IK — is not expressible against baked clips at all, so there is nothing to reproduce.
    /// </summary>
    public class CrowdAnimationModule : ActorModule, IAnimationDriver
    {
        /// <summary>A float parameter blending two clips. This is the locomotion blend tree's replacement.</summary>
        [Serializable]
        public class BlendBinding
        {
            public string ParameterName = "Movement";
            [Tooltip("Clip shown when the parameter is 0.")]
            public string ClipAtZero = "Idle";
            [Tooltip("Clip shown when the parameter is 1.")]
            public string ClipAtOne = "Run";
            public float FadeDuration = 0.15f;

            [NonSerialized] public int ParameterHash;
            [NonSerialized] public int ClipAtZeroHash;
            [NonSerialized] public int ClipAtOneHash;
            [NonSerialized] public float Value;
        }

        /// <summary>A bool parameter that holds a clip while it is true.</summary>
        [Serializable]
        public class StateBinding
        {
            public string ParameterName;
            public string ClipName;
            [Tooltip("Higher wins when several states are true at once.")]
            public int Priority;
            public float FadeDuration = 0.1f;

            [NonSerialized] public int ParameterHash;
            [NonSerialized] public int ClipHash;
            [NonSerialized] public bool Active;
        }

        /// <summary>A trigger parameter that plays a clip once, then releases control.</summary>
        [Serializable]
        public class TriggerBinding
        {
            public string ParameterName;
            public string ClipName;
            public float FadeDuration = 0.08f;

            [NonSerialized] public int ParameterHash;
            [NonSerialized] public int ClipHash;
        }

        [Header("Bindings")]
        [SerializeField] private BlendBinding Locomotion = new BlendBinding();
        [SerializeField] private List<StateBinding> States = new List<StateBinding>();
        [SerializeField] private List<TriggerBinding> Triggers = new List<TriggerBinding>();

        private AnimationModule _animationModule;
        private CrowdAgentRenderer _agentRenderer;

        private readonly Dictionary<int, StateBinding> _statesByParameter = new Dictionary<int, StateBinding>();
        private readonly Dictionary<int, TriggerBinding> _triggersByParameter = new Dictionary<int, TriggerBinding>();

        private TriggerBinding _pendingTrigger;
        private bool _oneShotRunning;

        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();

            CacheHashes();

            _animationModule = Actor.GetModule<AnimationModule>();
            if (_animationModule != null) _animationModule.Driver = this;

            ActorVisualHandler visualHandler = Actor.GetModule<ActorVisualHandler>();
            if (visualHandler == null) return;

            visualHandler.OnActorVisualSet += OnActorVisualSet;
            visualHandler.OnActorVisualRemoved += OnActorVisualRemoved;
            OnActorVisualSet(visualHandler.CurrentActorVisual);
        }

        public override void ModuleUpdate(float deltaTime)
        {
            // Read the animator through the renderer every frame rather than caching it: the visual is
            // activated after OnActorVisualSet fires, so the agent does not exist yet at hook-up time.
            CrowdAnimator animator = _agentRenderer != null ? _agentRenderer.Animator : null;
            if (animator == null) return;

            StateBinding state = GetActiveState();
            if (state != null)
            {
                // A state cancels anything queued or running — this is what makes death interrupt an attack.
                _pendingTrigger = null;
                _oneShotRunning = false;
                PlaySingle(animator, state.ClipHash, state.FadeDuration);
                return;
            }

            if (_pendingTrigger != null)
            {
                // Restart rather than Play: a second attack has to replay the clip even though it is already
                // the current one, which is exactly the case Play deliberately ignores.
                animator.ClearBlend();
                animator.Restart(_pendingTrigger.ClipHash, _pendingTrigger.FadeDuration);
                _oneShotRunning = true;
                _pendingTrigger = null;
                return;
            }

            if (_oneShotRunning)
            {
                if (!animator.IsFinished) return;
                _oneShotRunning = false;
            }

            ApplyLocomotion(animator);
        }

        public override void ResetModule()
        {
            base.ResetModule();

            // A pooled body must not wake up still flagged dead.
            foreach (StateBinding state in States) state.Active = false;
            _pendingTrigger = null;
            _oneShotRunning = false;
            Locomotion.Value = 0f;

            if (_agentRenderer != null && _agentRenderer.Animator != null) _agentRenderer.Animator.Reset();
        }

        public override void Cleanup()
        {
            base.Cleanup();

            if (_animationModule != null && ReferenceEquals(_animationModule.Driver, this))
                _animationModule.Driver = null;

            ActorVisualHandler visualHandler = Actor != null ? Actor.GetModule<ActorVisualHandler>() : null;
            if (visualHandler == null) return;

            visualHandler.OnActorVisualSet -= OnActorVisualSet;
            visualHandler.OnActorVisualRemoved -= OnActorVisualRemoved;
        }

        // ─── IAnimationDriver ─────────────────────────────────────────────────────
        // These are the writes AnimationModule would have made to an Animator. Nothing is applied here;
        // they only record intent, and ModuleUpdate resolves it once per frame against the binding rules.

        public void SetFloat(int parameterHash, float value)
        {
            if (parameterHash == Locomotion.ParameterHash) Locomotion.Value = value;
        }

        public void SetBool(int parameterHash, bool value)
        {
            if (_statesByParameter.TryGetValue(parameterHash, out StateBinding state)) state.Active = value;
        }

        public void SetTrigger(int parameterHash)
        {
            if (_triggersByParameter.TryGetValue(parameterHash, out TriggerBinding trigger)) _pendingTrigger = trigger;
        }

        public void SetInteger(int parameterHash, int value)
        {
            // Integer parameters pick between variants of a state inside an animator controller. Baked clips
            // are addressed by name, so a variant is simply its own binding and there is nothing to do here.
        }

        // ─── Internals ────────────────────────────────────────────────────────────

        private void CacheHashes()
        {
            Locomotion.ParameterHash = Animator.StringToHash(Locomotion.ParameterName);
            Locomotion.ClipAtZeroHash = Animator.StringToHash(Locomotion.ClipAtZero);
            Locomotion.ClipAtOneHash = Animator.StringToHash(Locomotion.ClipAtOne);

            _statesByParameter.Clear();
            foreach (StateBinding state in States)
            {
                if (string.IsNullOrEmpty(state.ParameterName)) continue;
                state.ParameterHash = Animator.StringToHash(state.ParameterName);
                state.ClipHash = Animator.StringToHash(state.ClipName);
                _statesByParameter[state.ParameterHash] = state;
            }

            _triggersByParameter.Clear();
            foreach (TriggerBinding trigger in Triggers)
            {
                if (string.IsNullOrEmpty(trigger.ParameterName)) continue;
                trigger.ParameterHash = Animator.StringToHash(trigger.ParameterName);
                trigger.ClipHash = Animator.StringToHash(trigger.ClipName);
                _triggersByParameter[trigger.ParameterHash] = trigger;
            }
        }

        private StateBinding GetActiveState()
        {
            StateBinding best = null;
            foreach (StateBinding state in States)
            {
                if (!state.Active) continue;
                if (best == null || state.Priority > best.Priority) best = state;
            }
            return best;
        }

        private static void PlaySingle(CrowdAnimator animator, int clipHash, float fadeDuration)
        {
            animator.ClearBlend();
            animator.Play(clipHash, fadeDuration);
        }

        private void ApplyLocomotion(CrowdAnimator animator)
        {
            if (Locomotion.ClipAtZeroHash == 0) return;

            // Called every frame; PlayBlend only fades when the base clip actually changes, so this settles
            // into just updating the weight.
            animator.PlayBlend(Locomotion.ClipAtZeroHash, Locomotion.ClipAtOneHash,
                Mathf.Clamp01(Locomotion.Value), Locomotion.FadeDuration);
        }

        private void OnActorVisualSet(ActorVisual visual)
        {
            _agentRenderer = visual != null ? visual.GetComponentInChildren<CrowdAgentRenderer>(true) : null;

            if (visual != null && _agentRenderer == null)
                Debug.LogWarning($"[{nameof(CrowdAnimationModule)}] No {nameof(CrowdAgentRenderer)} on the visual; " +
                                 "this actor will not animate.", visual);
        }

        private void OnActorVisualRemoved(ActorVisual visual) => _agentRenderer = null;
    }
}
