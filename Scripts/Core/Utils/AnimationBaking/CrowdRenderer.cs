using System.Collections.Generic;
using System.Runtime.InteropServices;
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
        /// Per-agent data as the shader sees it. The layout has to match the CrowdAgentData struct in
        /// CrowdSkinning.hlsl exactly — 32 bytes, two float4s, which also keeps the buffer stride aligned.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct AgentGpuData
        {
            public float Frame0;
            public float Frame1;
            public float Weight;
            public float Padding;
            public Vector4 Effect;
        }

        /// <summary>
        /// All agents sharing one animation set. Owns the CPU-side arrays and the GPU buffers, split into
        /// chunks of <see cref="MaxInstancesPerDraw"/>. Each chunk gets its own buffer and property block so
        /// instance id zero always means "first agent of this draw" and no offset uniform is needed.
        /// </summary>
        private sealed class Batch
        {
            private const int GpuDataStride = 32; // sizeof(AgentGpuData)

            private readonly CrowdAnimationSet _set;
            private readonly List<CrowdInstance> _instances = new List<CrowdInstance>();
            private readonly List<Chunk> _chunks = new List<Chunk>();

            public Batch(CrowdAnimationSet set) => _set = set;

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
                    chunk.Data[written] = new AgentGpuData
                    {
                        Frame0 = instance.Animator.Frame0,
                        Frame1 = instance.Animator.Frame1,
                        Weight = instance.Animator.Weight,
                        Effect = instance.EffectData,
                    };
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

            /// <summary>One instanced draw call worth of agents, with the buffers that back it.</summary>
            private sealed class Chunk
            {
                public readonly Matrix4x4[] Matrices = new Matrix4x4[MaxInstancesPerDraw];
                public readonly AgentGpuData[] Data = new AgentGpuData[MaxInstancesPerDraw];

                private readonly GraphicsBuffer _dataBuffer;
                private readonly MaterialPropertyBlock _propertyBlock;
                private Bounds _bounds;

                public Chunk(CrowdAnimationSet set)
                {
                    _dataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, MaxInstancesPerDraw, GpuDataStride);
                    _propertyBlock = new MaterialPropertyBlock();

                    set.ApplyStaticProperties(_propertyBlock);
                    _propertyBlock.SetBuffer(CrowdAnimationSet.AgentDataId, _dataBuffer);
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
                    _dataBuffer.SetData(Data, 0, 0, count);

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

                public void Dispose() => _dataBuffer?.Dispose();
            }
        }
    }
}
