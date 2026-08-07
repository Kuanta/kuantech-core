using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kuantech.Core.Utils
{
    /// <summary>
    /// Handle returned by <see cref="CrowdRenderer.Register"/>. Holds the agent's transform and playback
    /// state, and remembers where it sits in its batch so unregistering stays O(1).
    /// </summary>
    public sealed class CrowdInstance
    {
        public Transform Transform;
        public CrowdAnimator Animator;
        public CrowdAnimationSet Set;

        /// <summary>Skips this agent without unregistering it — for a body that is pooled but hidden.</summary>
        public bool Visible = true;

        /// <summary>
        /// Four floats handed to the shader for this agent alone, for effects that have to differ per agent:
        /// a hit flash, a dissolve, a tint strength. It exists because the usual way of driving such an effect
        /// — cloning the material and setting a property on the clone — cannot work here. Sharing one material
        /// is what collapses the whole crowd into a single draw call, so a per-agent material would trade the
        /// entire point of the system for one flashing goblin.
        ///
        /// Only values that genuinely vary per agent belong here. Anything the whole crowd shares (the flash
        /// colour, a ramp texture) is an ordinary material property and costs nothing.
        /// </summary>
        public Vector4 EffectData;

        internal int Index = -1;
    }

    /// <summary>
    /// Draws every baked agent in the scene. One batch per <see cref="CrowdAnimationSet"/>, one instanced
    /// draw call per 1023 agents inside it, and one loop that ticks all their animators.
    ///
    /// This is where the win actually lands. A hundred SkinnedMeshRenderers means a hundred Animator
    /// evaluations, a hundred skinning passes and a hundred draws; here the animators are three float
    /// operations each, the skinning happens for free in the vertex shader, and the draws collapse into one.
    ///
    /// The per-agent data (which two frames, and the weight between them) goes to the GPU in a
    /// StructuredBuffer indexed by instance id, because Shader Graph has no way to declare a per-instance
    /// property. The transforms travel the normal instancing path as a Matrix4x4 array.
    /// </summary>
    [DefaultExecutionOrder(100)] // after gameplay has moved the agents for this frame
    public class CrowdRenderer : MonoBehaviour
    {
        /// <summary>Instances per draw call. Matches Unity's limit for the instanced-array draw path.</summary>
        public const int MaxInstancesPerDraw = 1023;

        [Header("Rendering")]
        [SerializeField] private ShadowCastingMode ShadowCasting = ShadowCastingMode.Off;
        [SerializeField] private bool ReceiveShadows = true;
        [Tooltip("Layer the agents are drawn on. Only affects rendering — gameplay layers are on the actors.")]
        [SerializeField] private int RenderLayer;

        [Header("Debug")]
        [Tooltip("Read-only: how many agents were drawn last frame.")]
        [SerializeField] private int DrawnAgents;
        [Tooltip("Read-only: how many instanced draw calls that took.")]
        [SerializeField] private int DrawCalls;
        [Tooltip("Seconds between stat log lines. 0 disables. Needs a CrowdRenderer placed in the scene — " +
                 "the auto-created one cannot be configured, and on a device the log is the only way to see these.")]
        [SerializeField] private float StatsLogInterval;

        private float _statsLogTimer;

        private static CrowdRenderer _instance;

        private readonly List<Batch> _batches = new List<Batch>();
        private readonly Dictionary<CrowdAnimationSet, Batch> _batchesBySet = new Dictionary<CrowdAnimationSet, Batch>();

        /// <summary>
        /// The scene's renderer, created on demand so callers never have to care whether one was placed in
        /// the scene. The object is not marked DontDestroyOnLoad: agents do not survive a scene change either.
        /// </summary>
        public static CrowdRenderer Instance
        {
            get
            {
                if (_instance != null) return _instance;

                _instance = FindAnyObjectByType<CrowdRenderer>();
                if (_instance == null)
                {
                    GameObject host = new GameObject(nameof(CrowdRenderer));
                    _instance = host.AddComponent<CrowdRenderer>();
                }
                return _instance;
            }
        }

        /// <summary>
        /// The renderer if one already exists, without creating it. Teardown paths must use this — spawning a
        /// GameObject from OnDestroy or while the application is quitting is an error in Unity.
        /// </summary>
        public static CrowdRenderer Existing => _instance;

        /// <summary>
        /// Adds an agent to its set's batch. The returned handle is what the caller keeps; its
        /// <see cref="CrowdInstance.Animator"/> is the Animator replacement to drive from gameplay code.
        /// </summary>
        public CrowdInstance Register(CrowdAnimationSet set, Transform agentTransform)
        {
            if (set == null || !set.IsValid || agentTransform == null)
            {
                Debug.LogError($"[{nameof(CrowdRenderer)}] Cannot register an agent: the set is null or incomplete.", agentTransform);
                return null;
            }

            CrowdInstance instance = new CrowdInstance
            {
                Transform = agentTransform,
                Animator = new CrowdAnimator(set),
                Set = set,
            };

            GetOrCreateBatch(set).Add(instance);
            return instance;
        }

        public void Unregister(CrowdInstance instance)
        {
            if (instance == null || instance.Set == null) return;
            if (_batchesBySet.TryGetValue(instance.Set, out Batch batch)) batch.Remove(instance);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"[{nameof(CrowdRenderer)}] A second renderer was found; destroying it.", this);
                Destroy(this);
                return;
            }
            _instance = this;
        }

        private void OnDestroy()
        {
            foreach (Batch batch in _batches) batch.Dispose();
            _batches.Clear();
            _batchesBySet.Clear();

            if (_instance == this) _instance = null;
        }

        // LateUpdate so agents are drawn at the position gameplay left them this frame, not one behind.
        private void LateUpdate()
        {
            float deltaTime = Time.deltaTime;
            DrawnAgents = 0;
            DrawCalls = 0;

            for (int i = 0; i < _batches.Count; i++)
            {
                Batch batch = _batches[i];
                batch.Tick(deltaTime);
                batch.Draw(ShadowCasting, ReceiveShadows, RenderLayer, ref DrawnAgents, ref DrawCalls);
            }

            LogStats(deltaTime);
        }

        /// <summary>
        /// Periodic counters, because on a device there is no inspector to read. Registered against drawn is
        /// the distinction that matters: agents that registered but never drew mean a culling or bounds
        /// problem, while nothing registering at all points back at the prefabs.
        /// </summary>
        private void LogStats(float deltaTime)
        {
            if (StatsLogInterval <= 0f) return;

            _statsLogTimer += deltaTime;
            if (_statsLogTimer < StatsLogInterval) return;
            _statsLogTimer = 0f;

            int registered = 0;
            foreach (Batch batch in _batches) registered += batch.Count;

            Debug.Log($"[{nameof(CrowdRenderer)}] batches={_batches.Count} registered={registered} " +
                      $"drawn={DrawnAgents} drawCalls={DrawCalls}");

            // What the build actually holds, and where the first agent is being placed. Between them these
            // separate "the data did not survive the build" from "the data is fine but the shader is not
            // putting it on screen" — the two cases a drawn-but-invisible crowd cannot tell apart.
            foreach (Batch batch in _batches) batch.LogDescription();
        }

        private Batch GetOrCreateBatch(CrowdAnimationSet set)
        {
            if (_batchesBySet.TryGetValue(set, out Batch batch)) return batch;

            batch = new Batch(set);
            _batchesBySet.Add(set, batch);
            _batches.Add(batch);
            return batch;
        }

        /// <summary>
        /// Texels of agent state per agent in the data texture. Must match CROWD_AGENT_TEXELS in
        /// CrowdSkinning.hlsl, along with what each one holds:
        ///     (0, agent) = frame0, frame1, weight, unused
        ///     (1, agent) = effect
        /// </summary>
        private const int TexelsPerAgent = 2;

        /// <summary>
        /// All agents sharing one animation set. Owns the CPU-side arrays and the GPU buffers, split into
        /// chunks of <see cref="MaxInstancesPerDraw"/>. Each chunk gets its own buffer and property block so
        /// instance id zero always means "first agent of this draw" and no offset uniform is needed.
        /// </summary>
        private sealed class Batch
        {
            private readonly CrowdAnimationSet _set;
            private readonly List<CrowdInstance> _instances = new List<CrowdInstance>();
            private readonly List<Chunk> _chunks = new List<Chunk>();

            public Batch(CrowdAnimationSet set) => _set = set;

            /// <summary>Agents registered to this batch, drawn or not.</summary>
            public int Count => _instances.Count;

            /// <summary>
            /// Dumps the set and the first agent's placement alongside every stats line. Repeating rather
            /// than logging once is deliberate: a device's logcat is a ring buffer, and a single line printed
            /// when the level loaded is gone by the time anyone reads it.
            /// </summary>
            public void LogDescription()
            {
                if (_set == null) return;

                string first = "none";
                for (int i = 0; i < _instances.Count; i++)
                {
                    CrowdInstance instance = _instances[i];
                    if (instance.Transform == null) continue;
                    first = $"pos={instance.Transform.position} scale={instance.Transform.lossyScale} " +
                            $"frames={instance.Animator.Frame0}/{instance.Animator.Frame1} w={instance.Animator.Weight:0.00}";
                    break;
                }

                Debug.Log($"[{nameof(CrowdRenderer)}] set '{_set.name}': {_set.DescribeRuntimeState()}");
                Debug.Log($"[{nameof(CrowdRenderer)}] first agent: {first}");
            }

            public void Add(CrowdInstance instance)
            {
                instance.Index = _instances.Count;
                _instances.Add(instance);
            }

            public void Remove(CrowdInstance instance)
            {
                int index = instance.Index;
                if (index < 0 || index >= _instances.Count || _instances[index] != instance) return;

                // Swap-and-pop: order does not matter, and it keeps removal off the hot path.
                int last = _instances.Count - 1;
                _instances[index] = _instances[last];
                _instances[index].Index = index;
                _instances.RemoveAt(last);

                instance.Index = -1;
            }

            public void Tick(float deltaTime)
            {
                for (int i = 0; i < _instances.Count; i++) _instances[i].Animator.Update(deltaTime);
            }

            public void Draw(ShadowCastingMode shadowCasting, bool receiveShadows, int layer, ref int drawnAgents, ref int drawCalls)
            {
                if (_instances.Count == 0) return;

                int written = 0;
                int chunkIndex = 0;
                Chunk chunk = GetChunk(chunkIndex);

                for (int i = 0; i < _instances.Count; i++)
                {
                    CrowdInstance instance = _instances[i];
                    if (!instance.Visible || instance.Transform == null) continue;

                    chunk.Matrices[written] = instance.Transform.localToWorldMatrix;
                    chunk.SetAgent(written, instance.Animator, instance.EffectData);
                    chunk.Encapsulate(instance.Transform.position, written == 0);
                    written++;

                    if (written < MaxInstancesPerDraw) continue;

                    chunk.Submit(_set, written, shadowCasting, receiveShadows, layer);
                    drawnAgents += written;
                    drawCalls++;

                    written = 0;
                    chunk = GetChunk(++chunkIndex);
                }

                if (written <= 0) return;

                chunk.Submit(_set, written, shadowCasting, receiveShadows, layer);
                drawnAgents += written;
                drawCalls++;
            }

            public void Dispose()
            {
                foreach (Chunk chunk in _chunks) chunk.Dispose();
                _chunks.Clear();
            }

            private Chunk GetChunk(int index)
            {
                while (_chunks.Count <= index) _chunks.Add(new Chunk(_set));
                return _chunks[index];
            }

            /// <summary>
            /// One instanced draw call worth of agents, with the storage that backs it.
            ///
            /// The per-agent state travels in a small texture rather than a StructuredBuffer. A buffer is the
            /// natural fit and was the original design, but it needs OpenGL ES 3.1 — on an ES 3.0 device the
            /// shader will not load at all and every agent silently disappears. The texture is two texels
            /// wide and one row per agent, so the shader indexes it straight by instance id.
            /// </summary>
            private sealed class Chunk
            {
                public readonly Matrix4x4[] Matrices = new Matrix4x4[MaxInstancesPerDraw];

                private readonly Color[] _pixels = new Color[MaxInstancesPerDraw * TexelsPerAgent];
                private readonly Texture2D _dataTexture;
                private readonly MaterialPropertyBlock _propertyBlock;
                private Bounds _bounds;

                public Chunk(CrowdAnimationSet set)
                {
                    // Full floats: frame indices run past what a half represents exactly once a set has a few
                    // thousand frames, and the effect slot carries HDR colour. 2 x 1023 x 16 bytes is 32 KB.
                    _dataTexture = new Texture2D(TexelsPerAgent, MaxInstancesPerDraw, TextureFormat.RGBAFloat, false, true)
                    {
                        name = "CrowdAgentData",
                        filterMode = FilterMode.Point,
                        wrapMode = TextureWrapMode.Clamp,
                        anisoLevel = 0,
                    };

                    _propertyBlock = new MaterialPropertyBlock();
                    set.ApplyStaticProperties(_propertyBlock);
                    _propertyBlock.SetTexture(CrowdAnimationSet.AgentTextureId, _dataTexture);
                }

                /// <summary>
                /// Writes one agent's row. The two texels and their contents have to match CROWD_AGENT_TEXELS
                /// and the reads in CrowdSkinning.hlsl.
                /// </summary>
                public void SetAgent(int index, CrowdAnimator animator, Vector4 effect)
                {
                    int texel = index * TexelsPerAgent;
                    _pixels[texel + 0] = new Color(animator.Frame0, animator.Frame1, animator.Weight, 0f);
                    _pixels[texel + 1] = new Color(effect.x, effect.y, effect.z, effect.w);
                }

                /// <summary>
                /// Grows the draw's world bounds to hold this agent. Culling uses these bounds for the whole
                /// draw call, so they have to cover every agent in it plus the reach of the baked poses.
                /// </summary>
                public void Encapsulate(Vector3 position, bool first)
                {
                    if (first) _bounds = new Bounds(position, Vector3.zero);
                    else _bounds.Encapsulate(position);
                }

                public void Submit(CrowdAnimationSet set, int count, ShadowCastingMode shadowCasting, bool receiveShadows, int layer)
                {
                    // Uploads the whole texture rather than the used rows: 32 KB a frame, against the
                    // bookkeeping a partial upload would need to stay correct as the agent count moves.
                    _dataTexture.SetPixels(_pixels);
                    _dataTexture.Apply(false, false);

                    Bounds worldBounds = _bounds;
                    worldBounds.Expand(set.LocalBounds.size);

                    RenderParams renderParams = new RenderParams(set.Material)
                    {
                        worldBounds = worldBounds,
                        matProps = _propertyBlock,
                        shadowCastingMode = shadowCasting,
                        receiveShadows = receiveShadows,
                        layer = layer,
                    };

                    Graphics.RenderMeshInstanced(renderParams, set.Mesh, 0, Matrices, count);
                }

                public void Dispose()
                {
                    if (_dataTexture != null) Object.Destroy(_dataTexture);
                }
            }
        }
    }
}
