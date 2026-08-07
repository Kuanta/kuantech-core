using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kuantech.Core.Utils
{
    /// <summary>
    /// One baked clip inside a <see cref="CrowdAnimationSet"/>. Frames of every clip live in the same
    /// texture back to back, so a clip is just a row range: <see cref="StartFrame"/> is where it begins
    /// and <see cref="FrameCount"/> is how many rows it owns.
    /// </summary>
    [Serializable]
    public class CrowdAnimationClip
    {
        public string Name;
        [Tooltip("Animator.StringToHash(Name) — cached so runtime lookups never touch strings.")]
        public int NameHash;
        [Tooltip("Row of this clip's first frame in the animation texture.")]
        public int StartFrame;
        public int FrameCount;
        [Tooltip("Length of the source clip in seconds. Playback speed is derived from this, not from FrameCount.")]
        public float Duration;
        public bool Loop;

        public int LastFrame => StartFrame + FrameCount - 1;
    }

    /// <summary>
    /// The output of the animation baker: everything needed to draw an animated character without an
    /// Animator or a SkinnedMeshRenderer.
    ///
    /// The idea is that skinning has exactly one per-frame unknown — the skin matrix of each bone
    /// (bone transform × bindpose). Everything else (vertex positions, weights, bindposes) is constant.
    /// Since an animation is deterministic, those matrices can be evaluated once in the editor and stored,
    /// which is what <see cref="AnimationTexture"/> holds. At runtime the vertex shader looks the matrices
    /// up and blends them, so the CPU only has to answer "which frame is this agent on".
    ///
    /// Texture layout — one texel is one float4, one bone matrix is 3 texels (the fourth row of an affine
    /// matrix is always 0,0,0,1 and is not stored):
    ///
    ///     x: bone index * 3 + row      → width  = BoneCount * 3
    ///     y: global frame index        → height = TotalFrames (all clips concatenated)
    ///
    /// It is sampled with Load (point, unfiltered) because we want exact texels; smoothing between frames
    /// is done explicitly by reading two rows and blending them.
    /// </summary>
    [CreateAssetMenu(fileName = "CrowdAnimationSet", menuName = "Kuantech/Animation Baking/Crowd Animation Set")]
    public class CrowdAnimationSet : ScriptableObject
    {
        [Header("Baked Data")]
        [Tooltip("Mesh copy with bone indices in UV2 and bone weights in UV3, drawn by a plain MeshRenderer.")]
        public Mesh Mesh;
        [Tooltip("Bone skin matrices for every frame of every clip. See the class summary for the layout.")]
        public Texture2D AnimationTexture;
        [Tooltip("Material using a shader that runs the CrowdSkin custom function in its vertex stage.")]
        public Material Material;

        [Header("Layout")]
        public int BoneCount;
        public int TotalFrames;
        [Tooltip("Bone influences per vertex the mesh was baked with (2 or 4). Fewer means less vertex work.")]
        public int BoneInfluences = 4;
        [Tooltip("Frames per second the clips were sampled at.")]
        public int FrameRate = 30;
        [Tooltip("Object-space bounds covering every baked pose. Bind-pose bounds would cull wrongly.")]
        public Bounds LocalBounds = new Bounds(Vector3.zero, Vector3.one * 2f);

        [Header("Clips")]
        public List<CrowdAnimationClip> Clips = new List<CrowdAnimationClip>();

        // Shader property ids, resolved once. Kept here so the renderer, the preview and the baker all
        // agree on the names without repeating string literals.
        public static readonly int AnimationTextureId = Shader.PropertyToID("_CrowdAnimationTexture");
        public static readonly int BoneCountId = Shader.PropertyToID("_CrowdBoneCount");

        /// <summary>
        /// Per-agent state for the current draw call. A texture rather than a StructuredBuffer because SSBOs
        /// need OpenGL ES 3.1, and ES 3.0 devices are still inside the target range — on those the shader
        /// simply fails to load and every agent goes invisible. Texture reads work everywhere.
        /// </summary>
        public static readonly int AgentTextureId = Shader.PropertyToID("_CrowdAgentTexture");

        private Dictionary<int, CrowdAnimationClip> _clipsByHash;

        /// <summary>True when the asset holds everything needed to render. Cheap guard for callers.</summary>
        public bool IsValid => Mesh != null && AnimationTexture != null && Material != null && Clips.Count > 0;

        public CrowdAnimationClip GetClip(int nameHash)
        {
            BuildLookup();
            return _clipsByHash.TryGetValue(nameHash, out CrowdAnimationClip clip) ? clip : null;
        }

        public CrowdAnimationClip GetClip(string clipName) => GetClip(Animator.StringToHash(clipName));

        /// <summary>First clip in the set — used as a fallback so an agent is never left without a pose.</summary>
        public CrowdAnimationClip DefaultClip => Clips.Count > 0 ? Clips[0] : null;

        /// <summary>
        /// Pushes the set's static data onto a property block. The per-agent data is written separately by
        /// the renderer; this is only the part that is the same for every instance of the set.
        /// </summary>
        /// <summary>
        /// What this set actually amounts to at runtime, for logging on a device. Asset references, mesh
        /// vertex channels and texture formats can all survive the editor and then differ in a player, and a
        /// crowd that draws but stays invisible looks exactly the same whichever of them went missing — so
        /// the only way to tell them apart is to have the build say what it is holding.
        /// </summary>
        public string DescribeRuntimeState()
        {
            string mesh = Mesh == null
                ? "NULL"
                : $"{Mesh.vertexCount}v uv2={Mesh.HasVertexAttribute(VertexAttribute.TexCoord2)} " +
                  $"uv3={Mesh.HasVertexAttribute(VertexAttribute.TexCoord3)}";

            string texture = AnimationTexture == null
                ? "NULL"
                : $"{AnimationTexture.width}x{AnimationTexture.height} {AnimationTexture.format} " +
                  $"filter={AnimationTexture.filterMode}";

            string material = Material == null
                ? "NULL"
                : $"{(Material.shader != null ? Material.shader.name : "NO SHADER")} instancing={Material.enableInstancing}";

            return $"mesh[{mesh}] animTex[{texture}] material[{material}] " +
                   $"bones={BoneCount} influences={BoneInfluences} clips={Clips.Count}";
        }

        public void ApplyStaticProperties(MaterialPropertyBlock block)
        {
            block.SetTexture(AnimationTextureId, AnimationTexture);
            block.SetFloat(BoneCountId, BoneCount);
        }

        private void BuildLookup()
        {
            if (_clipsByHash != null && _clipsByHash.Count == Clips.Count) return;

            _clipsByHash = new Dictionary<int, CrowdAnimationClip>(Clips.Count);
            foreach (CrowdAnimationClip clip in Clips)
            {
                if (clip == null) continue;
                _clipsByHash[clip.NameHash] = clip;
            }
        }

        private void OnValidate()
        {
            // Hashes are written by the baker, but the list is editable by hand — keep them in sync.
            foreach (CrowdAnimationClip clip in Clips)
            {
                if (clip != null && !string.IsNullOrEmpty(clip.Name)) clip.NameHash = Animator.StringToHash(clip.Name);
            }
            _clipsByHash = null;
        }
    }
}
