using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Kuantech.Core.Utils
{
    /// <summary>
    /// Turns a rigged character plus a set of animation clips into a <see cref="CrowdAnimationSet"/>.
    ///
    /// The whole system exists because steps 1-4 of Unity's per-frame skinning work — evaluate the state
    /// machine, write the bone transforms, walk the hierarchy, build the skin matrices — are deterministic.
    /// The answer to "where is this bone on frame 12 of Attack" never changes, so there is no reason to
    /// recompute it sixty times a second for a hundred agents. This baker computes it once and writes the
    /// result to a texture; the runtime then only has to pick a row.
    ///
    /// Sampling goes through <see cref="AnimationMode"/> rather than AnimationClip.SampleAnimation because
    /// that is the path the Animation window scrubs with, and it applies Humanoid retargeting. That is what
    /// makes the rig type a non-issue: the character stays Humanoid so it can keep using retargeted animation
    /// packs, and the retargeting is resolved here, in the editor. Nothing Humanoid survives into the build.
    ///
    /// Root motion is intentionally not baked. Bone matrices are stored relative to the character root, so
    /// whatever the clip does to the root cancels out and movement stays owned by gameplay code.
    /// </summary>
    public static class CrowdAnimationBaker
    {
        /// <summary>Texels per bone matrix — three rows of an affine transform, the fourth is implicit.</summary>
        private const int TexelsPerMatrix = 3;

        public class BakeSettings
        {
            [Tooltip("Prefab or scene object carrying the Animator and the SkinnedMeshRenderer to bake.")]
            public GameObject Source;
            public List<AnimationClip> Clips = new List<AnimationClip>();

            [Tooltip("Sampling rate. 30 is plenty because the shader interpolates between rows.")]
            public int FrameRate = 30;
            [Tooltip("Bone influences kept per vertex. 2 halves the shader's texture reads; pick the matching CrowdSkin node.")]
            public int BoneInfluences = 4;
            [Tooltip("Store matrices as full floats. Only needed for very large characters where half precision shows.")]
            public bool HighPrecision;

            public string OutputFolder = "Assets";
            public string AssetName = "CrowdAnimationSet";
        }

        /// <summary>
        /// Runs a bake and writes the result to disk. Returns null and fills <paramref name="error"/> if the
        /// input cannot produce a valid set. Re-baking over an existing asset keeps the asset itself — and
        /// therefore every reference to it, including the assigned material — and replaces only the data.
        /// </summary>
        public static CrowdAnimationSet Bake(BakeSettings settings, out string error)
        {
            if (!Validate(settings, out error)) return null;

            GameObject instance = Object.Instantiate(settings.Source);
            instance.name = $"__CrowdBake_{settings.Source.name}";
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            try
            {
                SkinnedMeshRenderer skinnedMesh = instance.GetComponentInChildren<SkinnedMeshRenderer>();
                Transform[] bones = skinnedMesh.bones;
                Matrix4x4[] bindPoses = skinnedMesh.sharedMesh.bindposes;

                if (bones.Length != bindPoses.Length)
                {
                    error = $"Bone count ({bones.Length}) does not match bindpose count ({bindPoses.Length}). " +
                            "The mesh and the rig do not belong together.";
                    return null;
                }

                List<CrowdAnimationClip> clipTable = new List<CrowdAnimationClip>();
                Matrix4x4[] frames = SampleClips(instance, settings, bones, bindPoses, clipTable);
                if (frames == null)
                {
                    error = "Sampling produced no frames.";
                    return null;
                }

                int boneCount = bones.Length;
                int totalFrames = frames.Length / boneCount;

                if (!ValidateTextureSize(boneCount, totalFrames, out error)) return null;

                Texture2D animationTexture = BuildAnimationTexture(frames, boneCount, totalFrames, settings.HighPrecision);
                Mesh bakedMesh = BuildMesh(skinnedMesh.sharedMesh, settings.BoneInfluences);
                Bounds bounds = CalculateBounds(skinnedMesh.sharedMesh, frames, boneCount, totalFrames);

                return WriteAsset(settings, animationTexture, bakedMesh, bounds, boneCount, totalFrames, clipTable);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Object.DestroyImmediate(instance);
            }
        }

        /// <summary>Pulls every distinct clip out of an animator controller, in a stable order.</summary>
        public static List<AnimationClip> CollectClips(RuntimeAnimatorController controller)
        {
            List<AnimationClip> clips = new List<AnimationClip>();
            if (controller == null) return clips;

            foreach (AnimationClip clip in controller.animationClips)
            {
                if (clip != null && !clips.Contains(clip)) clips.Add(clip);
            }
            return clips;
        }

        private static bool Validate(BakeSettings settings, out string error)
        {
            error = null;

            if (settings.Source == null)
            {
                error = "No source object assigned.";
                return false;
            }
            if (settings.Source.GetComponentInChildren<Animator>() == null)
            {
                error = "The source has no Animator. Sampling needs one, Humanoid rigs especially.";
                return false;
            }
            if (settings.Source.GetComponentInChildren<SkinnedMeshRenderer>() == null)
            {
                error = "The source has no SkinnedMeshRenderer.";
                return false;
            }
            if (settings.Clips == null || settings.Clips.Count == 0)
            {
                error = "No clips to bake.";
                return false;
            }
            if (settings.BoneInfluences != 2 && settings.BoneInfluences != 4)
            {
                error = "Bone influences must be 2 or 4.";
                return false;
            }
            if (!AssetDatabase.IsValidFolder(settings.OutputFolder))
            {
                error = $"Output folder does not exist: {settings.OutputFolder}";
                return false;
            }
            return true;
        }

        private static bool ValidateTextureSize(int boneCount, int totalFrames, out string error)
        {
            error = null;
            int width = boneCount * TexelsPerMatrix;
            int maxSize = SystemInfo.maxTextureSize;

            if (width <= maxSize && totalFrames <= maxSize) return true;

            error = $"The baked texture would be {width}x{totalFrames}, over this platform's {maxSize} limit. " +
                    "Lower the frame rate or bake fewer clips per set.";
            return false;
        }

        /// <summary>
        /// Walks every clip frame by frame and records each bone's skin matrix. The returned array is laid
        /// out frame-major (frame * boneCount + bone), which matches the texture rows one to one.
        /// </summary>
        private static Matrix4x4[] SampleClips(GameObject root, BakeSettings settings, Transform[] bones,
                                               Matrix4x4[] bindPoses, List<CrowdAnimationClip> clipTable)
        {
            int boneCount = bones.Length;
            List<Matrix4x4> frames = new List<Matrix4x4>();
            int frameCursor = 0;

            AnimationMode.StartAnimationMode();
            try
            {
                for (int clipIndex = 0; clipIndex < settings.Clips.Count; clipIndex++)
                {
                    AnimationClip clip = settings.Clips[clipIndex];
                    if (clip == null) continue;

                    bool looping = clip.isLooping;
                    int frameCount = Mathf.Max(1, Mathf.CeilToInt(clip.length * settings.FrameRate));

                    EditorUtility.DisplayProgressBar("Baking crowd animation", clip.name,
                        clipIndex / (float)settings.Clips.Count);

                    for (int frame = 0; frame < frameCount; frame++)
                    {
                        // A looping clip's last frame is the first frame again, so it is sampled over [0, length)
                        // and left out. A one-shot has to include its final pose, so it spans [0, length].
                        float phase = looping
                            ? frame / (float)frameCount
                            : (frameCount > 1 ? frame / (float)(frameCount - 1) : 0f);

                        // Reset first: sampling writes root motion into the root, and letting that accumulate
                        // across samples would slowly drift the pose away from the origin.
                        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                        AnimationMode.BeginSampling();
                        AnimationMode.SampleAnimationClip(root, clip, phase * clip.length);
                        AnimationMode.EndSampling();

                        // Relative to the root, which drops root motion and leaves pure pose.
                        Matrix4x4 rootInverse = root.transform.worldToLocalMatrix;
                        for (int bone = 0; bone < boneCount; bone++)
                        {
                            frames.Add(rootInverse * bones[bone].localToWorldMatrix * bindPoses[bone]);
                        }
                    }

                    clipTable.Add(new CrowdAnimationClip
                    {
                        Name = clip.name,
                        NameHash = Animator.StringToHash(clip.name),
                        StartFrame = frameCursor,
                        FrameCount = frameCount,
                        Duration = Mathf.Max(clip.length, 0.0001f),
                        Loop = looping,
                    });

                    frameCursor += frameCount;
                }
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }

            return frames.Count == 0 ? null : frames.ToArray();
        }

        /// <summary>
        /// Packs the sampled matrices into the texture layout the shader reads: three texels per bone matrix
        /// along x, one frame per row along y.
        /// </summary>
        private static Texture2D BuildAnimationTexture(Matrix4x4[] frames, int boneCount, int totalFrames, bool highPrecision)
        {
            int width = boneCount * TexelsPerMatrix;
            Color[] pixels = new Color[width * totalFrames];

            for (int frame = 0; frame < totalFrames; frame++)
            {
                int rowStart = frame * width;
                int matrixStart = frame * boneCount;

                for (int bone = 0; bone < boneCount; bone++)
                {
                    Matrix4x4 m = frames[matrixStart + bone];
                    int texel = rowStart + bone * TexelsPerMatrix;

                    pixels[texel + 0] = new Color(m.m00, m.m01, m.m02, m.m03);
                    pixels[texel + 1] = new Color(m.m10, m.m11, m.m12, m.m13);
                    pixels[texel + 2] = new Color(m.m20, m.m21, m.m22, m.m23);
                }
            }

            TextureFormat format = highPrecision ? TextureFormat.RGBAFloat : TextureFormat.RGBAHalf;
            Texture2D texture = new Texture2D(width, totalFrames, format, false, true)
            {
                name = "CrowdAnimationTexture",
                // Point filtering is not a style choice: the rows are matrix data, and letting the hardware
                // blend neighbouring texels would mix unrelated bones together.
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 0,
            };

            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }

        /// <summary>
        /// Copies the source mesh and moves its skinning data into UV channels, because a MeshRenderer does
        /// not upload bone weights — that is a SkinnedMeshRenderer feature. UV channels are just four floats
        /// per vertex; nothing requires them to hold texture coordinates.
        /// </summary>
        private static Mesh BuildMesh(Mesh source, int influences)
        {
            Mesh mesh = Object.Instantiate(source);
            mesh.name = "CrowdMesh";

            BoneWeight[] weights = source.boneWeights;
            int vertexCount = source.vertexCount;

            List<Vector4> boneIndices = new List<Vector4>(vertexCount);
            List<Vector4> boneWeights = new List<Vector4>(vertexCount);

            for (int i = 0; i < vertexCount; i++)
            {
                BoneWeight w = weights[i];

                // Unity keeps BoneWeight influences sorted by weight, so trimming to two is just dropping
                // the tail and renormalising what is left.
                Vector4 indices = new Vector4(w.boneIndex0, w.boneIndex1, w.boneIndex2, w.boneIndex3);
                Vector4 values = new Vector4(w.weight0, w.weight1, w.weight2, w.weight3);

                if (influences == 2)
                {
                    values.z = 0f;
                    values.w = 0f;
                }

                float total = values.x + values.y + values.z + values.w;
                if (total > 0f) values /= total;
                else values = new Vector4(1f, 0f, 0f, 0f);

                boneIndices.Add(indices);
                boneWeights.Add(values);
            }

            mesh.SetUVs(2, boneIndices);
            mesh.SetUVs(3, boneWeights);

            // Drop the skinning data itself: it is now in the UVs, and a MeshRenderer would only carry it
            // around as dead weight. Blend shapes cannot survive the bake either.
            mesh.ClearBlendShapes();
            mesh.boneWeights = new BoneWeight[0];
            mesh.bindposes = new Matrix4x4[0];

            return mesh;
        }

        /// <summary>
        /// Finds the object-space box that holds every baked pose. Bind-pose bounds are not enough: an attack
        /// swing reaches well outside a T-pose, and culling that used the smaller box would pop the agent out
        /// of view mid-animation.
        /// </summary>
        private static Bounds CalculateBounds(Mesh source, Matrix4x4[] frames, int boneCount, int totalFrames)
        {
            Vector3[] vertices = source.vertices;
            BoneWeight[] weights = source.boneWeights;

            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.negativeInfinity;

            for (int frame = 0; frame < totalFrames; frame++)
            {
                int matrixStart = frame * boneCount;

                for (int i = 0; i < vertices.Length; i++)
                {
                    BoneWeight w = weights[i];
                    Vector3 v = vertices[i];

                    Vector3 skinned =
                        frames[matrixStart + w.boneIndex0].MultiplyPoint3x4(v) * w.weight0 +
                        frames[matrixStart + w.boneIndex1].MultiplyPoint3x4(v) * w.weight1 +
                        frames[matrixStart + w.boneIndex2].MultiplyPoint3x4(v) * w.weight2 +
                        frames[matrixStart + w.boneIndex3].MultiplyPoint3x4(v) * w.weight3;

                    min = Vector3.Min(min, skinned);
                    max = Vector3.Max(max, skinned);
                }
            }

            Bounds bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return bounds;
        }

        private static CrowdAnimationSet WriteAsset(BakeSettings settings, Texture2D texture, Mesh mesh, Bounds bounds,
                                                    int boneCount, int totalFrames, List<CrowdAnimationClip> clips)
        {
            string path = Path.Combine(settings.OutputFolder, settings.AssetName + ".asset").Replace('\\', '/');
            CrowdAnimationSet set = AssetDatabase.LoadAssetAtPath<CrowdAnimationSet>(path);

            if (set == null)
            {
                set = ScriptableObject.CreateInstance<CrowdAnimationSet>();
                AssetDatabase.CreateAsset(set, path);
            }
            else
            {
                // Re-bake: strip the old mesh and texture but keep the asset, so prefabs pointing at this set
                // and the material assigned to it survive.
                foreach (Object sub in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (sub == set || sub == null) continue;
                    AssetDatabase.RemoveObjectFromAsset(sub);
                    Object.DestroyImmediate(sub, true);
                }
            }

            set.Mesh = mesh;
            set.AnimationTexture = texture;
            set.BoneCount = boneCount;
            set.TotalFrames = totalFrames;
            set.BoneInfluences = settings.BoneInfluences;
            set.FrameRate = settings.FrameRate;
            set.LocalBounds = bounds;
            set.Clips = clips;

            AssetDatabase.AddObjectToAsset(mesh, set);
            AssetDatabase.AddObjectToAsset(texture, set);

            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path);

            return AssetDatabase.LoadAssetAtPath<CrowdAnimationSet>(path);
        }
    }
}
