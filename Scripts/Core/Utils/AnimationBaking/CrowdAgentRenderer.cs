using UnityEngine;

namespace Kuantech.Core.Utils
{
    /// <summary>
    /// Makes one GameObject render as a baked crowd agent. This is the counterpart of a
    /// SkinnedMeshRenderer, and it is deliberately the only component a character needs.
    ///
    /// It does not draw anything itself. All it does is hand its transform to <see cref="CrowdRenderer"/>,
    /// which reads the transform every frame and folds this agent into one instanced draw call shared with
    /// every other agent of the same set. Whatever moves the transform — a NavMesh agent, a movement module,
    /// a scripted flight — the visual follows for free, exactly as it would with a real renderer attached.
    ///
    /// The mesh, the material and the animation data all live inside the <see cref="CrowdAnimationSet"/>, so
    /// that asset is the whole configuration: assigning it answers "which character is this".
    ///
    /// Registration is tied to OnEnable/OnDisable rather than the object's lifetime, so a pooled body that
    /// is deactivated leaves the crowd and rejoins it when it is handed out again.
    /// </summary>
    [DisallowMultipleComponent]
    public class CrowdAgentRenderer : MonoBehaviour
    {
        [Tooltip("Baked character to render. Holds the mesh, the material and every clip.")]
        [SerializeField] private CrowdAnimationSet Set;

        [Tooltip("Clip to start on. Empty falls back to the first clip in the set.")]
        [SerializeField] private string StartClip;

        [Tooltip("Start at a random point in the clip so a horde of identical agents does not move in lockstep.")]
        [SerializeField] private bool RandomizePhase = true;

        private CrowdInstance _instance;

        /// <summary>
        /// Playback control for this agent — the Animator replacement. Null before the first OnEnable, and
        /// while the agent is disabled, so gameplay code should null-check it like it would an Animator.
        /// </summary>
        public CrowdAnimator Animator => _instance?.Animator;

        public CrowdAnimationSet AnimationSet => Set;

        /// <summary>
        /// Hides the agent without leaving the crowd. Cheaper than disabling the component when the body is
        /// only briefly invisible, since it keeps the registration and the playback state intact.
        /// </summary>
        public bool Visible
        {
            get => _instance != null && _instance.Visible;
            set { if (_instance != null) _instance.Visible = value; }
        }

        /// <summary>
        /// Four floats this agent alone hands to the shader — a hit flash amount, a dissolve, a tint. This is
        /// how a per-agent shader effect is driven here: the usual route of cloning the material would give
        /// every agent its own material and break the single draw call the whole system is built around.
        /// See <see cref="CrowdInstance.EffectData"/>.
        /// </summary>
        public Vector4 EffectData
        {
            get => _instance != null ? _instance.EffectData : Vector4.zero;
            set { if (_instance != null) _instance.EffectData = value; }
        }

        /// <summary>
        /// Swaps to a different baked character at runtime. The agent leaves its old batch and joins the new
        /// one, which also means playback restarts — a set change is a different skeleton, not a new pose.
        /// </summary>
        public void SetAnimationSet(CrowdAnimationSet set)
        {
            if (Set == set) return;

            Leave();
            Set = set;
            if (isActiveAndEnabled) Join();
        }

        private void OnEnable() => Join();

        private void OnDisable() => Leave();

        private void Join()
        {
            if (Set == null)
            {
                Debug.LogError($"[{nameof(CrowdAgentRenderer)}] No animation set assigned; nothing will render.", this);
                return;
            }

            _instance = CrowdRenderer.Instance.Register(Set, transform);
            if (_instance == null) return;

            CrowdAnimationClip clip = string.IsNullOrEmpty(StartClip) ? Set.DefaultClip : Set.GetClip(StartClip);
            if (clip == null) return;

            _instance.Animator.Play(clip.NameHash, 0f);
            if (RandomizePhase) _instance.Animator.Update(Random.Range(0f, clip.Duration));
        }

        private void Leave()
        {
            if (_instance == null) return;

            // Existing rather than Instance: this also runs during teardown, where creating the renderer
            // would spawn a GameObject at a point Unity does not allow it.
            CrowdRenderer renderer = CrowdRenderer.Existing;
            if (renderer != null) renderer.Unregister(_instance);

            _instance = null;
        }
    }
}
