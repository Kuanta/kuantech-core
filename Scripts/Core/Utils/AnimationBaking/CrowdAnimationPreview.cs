using UnityEngine;

namespace Kuantech.Core.Utils
{
    /// <summary>
    /// Verification harness for a baked set, with no gameplay attached. Two things need proving before the
    /// system is wired into anything real, and this covers both:
    ///
    ///   correctness — one agent playing one clip. If the bake, the texture layout and the shader agree, it
    ///                 looks exactly like the original SkinnedMeshRenderer did.
    ///   scale       — a grid of agents. This is the measurement that justifies the whole system, so it is
    ///                 worth running on the target device rather than in the editor.
    ///
    /// Clip name and speed can be changed while playing; the change is applied through the same
    /// <see cref="CrowdAnimator"/> API gameplay code would use.
    /// </summary>
    public class CrowdAnimationPreview : MonoBehaviour
    {
        [SerializeField] private CrowdAnimationSet Set;
        [Tooltip("Clip to play. Empty falls back to the first clip in the set.")]
        [SerializeField] private string ClipName;
        [SerializeField] private float Speed = 1f;

        [Header("Stress Test")]
        [Tooltip("Agents per side of the grid. 1 draws a single agent on this transform.")]
        [SerializeField] private int GridSize = 1;
        [SerializeField] private float Spacing = 1.5f;
        [Tooltip("Start each agent at a random point in the clip so the grid does not move as one block.")]
        [SerializeField] private bool RandomizePhase = true;

        private CrowdInstance[] _instances;

        private void Start()
        {
            if (Set == null || !Set.IsValid)
            {
                Debug.LogError($"[{nameof(CrowdAnimationPreview)}] Assign a set with a mesh, texture and material.", this);
                enabled = false;
                return;
            }

            _instances = GridSize > 1 ? SpawnGrid() : new[] { CrowdRenderer.Instance.Register(Set, transform) };
            ApplySettings();
        }

        private void OnDestroy()
        {
            CrowdRenderer renderer = CrowdRenderer.Existing;
            if (_instances == null || renderer == null) return;

            foreach (CrowdInstance instance in _instances)
            {
                if (instance != null) renderer.Unregister(instance);
            }
            _instances = null;
        }

        private void OnValidate()
        {
            if (Application.isPlaying && _instances != null) ApplySettings();
        }

        private CrowdInstance[] SpawnGrid()
        {
            int count = GridSize * GridSize;
            CrowdInstance[] instances = new CrowdInstance[count];
            float offset = (GridSize - 1) * Spacing * 0.5f;

            for (int i = 0; i < count; i++)
            {
                int x = i % GridSize;
                int z = i / GridSize;

                // Empty transforms only — the agents have no renderer of their own; CrowdRenderer draws
                // them all from one instanced call and only reads their matrix.
                GameObject agent = new GameObject($"Agent_{i}");
                agent.transform.SetParent(transform, false);
                agent.transform.localPosition = new Vector3(x * Spacing - offset, 0f, z * Spacing - offset);

                instances[i] = CrowdRenderer.Instance.Register(Set, agent.transform);
            }

            return instances;
        }

        private void ApplySettings()
        {
            CrowdAnimationClip clip = string.IsNullOrEmpty(ClipName) ? Set.DefaultClip : Set.GetClip(ClipName);
            if (clip == null)
            {
                Debug.LogWarning($"[{nameof(CrowdAnimationPreview)}] No clip named '{ClipName}' in {Set.name}.", this);
                return;
            }

            foreach (CrowdInstance instance in _instances)
            {
                if (instance == null) continue;

                instance.Animator.Play(clip.NameHash);
                instance.Animator.Speed = Speed;

                // Desync the grid: without this every agent hits the same pose on the same frame, which both
                // looks wrong and hides any per-agent bug behind the uniformity.
                if (RandomizePhase && GridSize > 1) instance.Animator.Update(Random.Range(0f, clip.Duration));
            }
        }
    }
}
