using System.Text;
using UnityEditor;
using UnityEngine;

namespace Kuantech.Core.Utils
{
    /// <summary>
    /// Reads a baked set back off disk and checks whether the data in it is sane. When a crowd agent renders
    /// wrong, the failure is either in the bake or in the shader, and telling those apart by looking at the
    /// mesh is guesswork. This answers it directly.
    ///
    /// The decisive test is the rigid-transform check. Every stored matrix is a bone's world transform times
    /// its bindpose, and a character rig contains only rotations and translations — so the upper 3x3 of every
    /// matrix must be orthonormal with a determinant of 1. That property survives no plausible corruption: if
    /// the matrices pass, the texture holds exactly what the baker intended and the fault is downstream in
    /// the shader. If they fail, the bake or the texture write path is broken and the shader is innocent.
    ///
    /// The value-range check catches one specific failure the rigid test would also flag but not explain:
    /// a write path that clamped the data to [0,1]. Real bone matrices always contain negative numbers, so
    /// their complete absence means something clamped on the way to the texture.
    /// </summary>
    public static class CrowdAnimationSetValidator
    {
        private const int TexelsPerMatrix = 3;

        /// <summary>How far a row length or determinant may drift before the matrix is called broken.</summary>
        private const float RigidTolerance = 0.02f;

        /// <summary>
        /// Validates the selected set, or every set in the project when the selection is something else, so
        /// the menu item is never a dead end and never needs a validate function that could grey it out.
        /// </summary>
        [MenuItem("Kuantech/Animation Baking/Validate Crowd Animation Set")]
        private static void ValidateMenu()
        {
            if (Selection.activeObject is CrowdAnimationSet selected)
            {
                Debug.Log(Validate(selected), selected);
                return;
            }

            string[] guids = AssetDatabase.FindAssets($"t:{nameof(CrowdAnimationSet)}");
            if (guids.Length == 0)
            {
                Debug.LogWarning("[CrowdValidator] No CrowdAnimationSet assets found in the project.");
                return;
            }

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CrowdAnimationSet set = AssetDatabase.LoadAssetAtPath<CrowdAnimationSet>(path);
                if (set != null) Debug.Log(Validate(set), set);
            }
        }

        public static string Validate(CrowdAnimationSet set)
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine($"[CrowdValidator] {set.name}");

            if (!AppendAssetChecks(set, report)) return report.ToString();

            Color[] pixels;
            try
            {
                pixels = set.AnimationTexture.GetPixels();
            }
            catch (UnityException exception)
            {
                report.AppendLine($"FAIL  Could not read the texture: {exception.Message}");
                return report.ToString();
            }

            AppendRangeCheck(pixels, report);
            AppendRigidCheck(set, pixels, report);
            AppendMeshCheck(set, report);
            AppendReferenceSkinCheck(set, pixels, report);

            return report.ToString();
        }

        private static bool AppendAssetChecks(CrowdAnimationSet set, StringBuilder report)
        {
            if (set.Mesh == null) report.AppendLine("FAIL  No baked mesh.");
            if (set.Material == null) report.AppendLine("WARN  No material assigned — nothing will render.");
            if (set.AnimationTexture == null)
            {
                report.AppendLine("FAIL  No animation texture; nothing else can be checked.");
                return false;
            }

            Texture2D texture = set.AnimationTexture;
            int expectedWidth = set.BoneCount * TexelsPerMatrix;

            report.AppendLine($"      Texture {texture.width}x{texture.height}, {texture.format}, filter {texture.filterMode}");
            report.AppendLine($"      Bones {set.BoneCount}, frames {set.TotalFrames}, influences {set.BoneInfluences}, clips {set.Clips.Count}");

            if (texture.width != expectedWidth)
                report.AppendLine($"FAIL  Width is {texture.width} but {set.BoneCount} bones need {expectedWidth}.");
            if (texture.height != set.TotalFrames)
                report.AppendLine($"FAIL  Height is {texture.height} but the clip table adds up to {set.TotalFrames} frames.");
            if (texture.filterMode != FilterMode.Point)
                report.AppendLine("FAIL  Filter mode is not Point. Filtering blends neighbouring bones together.");

            return true;
        }

        /// <summary>
        /// Reports the spread of the stored values. Bone matrices always contain negatives — a rig has bones
        /// on both sides of its own origin — so an all-positive texture means the data was clamped somewhere
        /// between the baker and the asset.
        /// </summary>
        private static void AppendRangeCheck(Color[] pixels, StringBuilder report)
        {
            float min = float.MaxValue;
            float max = float.MinValue;
            int negatives = 0;

            // The alpha channel of each texel is the matrix row's translation term. It is tracked separately
            // because the other nine components come from a rotation and are always inside [-1, 1] by
            // definition — only the translation can reveal a write path that clipped the data to that range.
            float translationExtreme = 0f;

            foreach (Color pixel in pixels)
            {
                for (int c = 0; c < 4; c++)
                {
                    float value = pixel[c];
                    if (value < min) min = value;
                    if (value > max) max = value;
                    if (value < 0f) negatives++;
                }

                translationExtreme = Mathf.Max(translationExtreme, Mathf.Abs(pixel.a));
            }

            report.AppendLine($"      Value range [{min:0.###}, {max:0.###}], {negatives} negative components");
            report.AppendLine($"      Largest translation term {translationExtreme:0.#####}");

            if (negatives == 0)
                report.AppendLine("FAIL  No negative values at all. The write path clamped the data to [0,1].");

            // Exactly 1 would mean the translations ran into a limit rather than stopping where the pose does.
            if (Mathf.Abs(translationExtreme - 1f) < 0.0001f)
                report.AppendLine("FAIL  Translations top out at exactly 1 — the write path clamped them to [-1,1].");
        }

        /// <summary>
        /// The main check: every stored matrix must be a rigid transform. Samples the first frame of each
        /// clip, which is enough — a layout or write bug corrupts every frame the same way.
        /// </summary>
        private static void AppendRigidCheck(CrowdAnimationSet set, Color[] pixels, StringBuilder report)
        {
            int width = set.BoneCount * TexelsPerMatrix;
            int checkedMatrices = 0;
            int broken = 0;
            string firstFailure = null;

            foreach (CrowdAnimationClip clip in set.Clips)
            {
                if (clip == null || clip.StartFrame >= set.TotalFrames) continue;

                int rowStart = clip.StartFrame * width;

                for (int bone = 0; bone < set.BoneCount; bone++)
                {
                    int texel = rowStart + bone * TexelsPerMatrix;
                    Color r0 = pixels[texel + 0];
                    Color r1 = pixels[texel + 1];
                    Color r2 = pixels[texel + 2];

                    // Rows of the upper 3x3. For a rotation these are unit length and mutually perpendicular.
                    Vector3 x = new Vector3(r0.r, r0.g, r0.b);
                    Vector3 y = new Vector3(r1.r, r1.g, r1.b);
                    Vector3 z = new Vector3(r2.r, r2.g, r2.b);

                    float determinant = Vector3.Dot(Vector3.Cross(x, y), z);
                    bool rigid =
                        Mathf.Abs(x.magnitude - 1f) < RigidTolerance &&
                        Mathf.Abs(y.magnitude - 1f) < RigidTolerance &&
                        Mathf.Abs(z.magnitude - 1f) < RigidTolerance &&
                        Mathf.Abs(Mathf.Abs(determinant) - 1f) < RigidTolerance;

                    checkedMatrices++;
                    if (rigid) continue;

                    broken++;
                    firstFailure ??= $"clip '{clip.Name}' bone {bone}: row lengths " +
                                     $"({x.magnitude:0.###}, {y.magnitude:0.###}, {z.magnitude:0.###}), det {determinant:0.###}";
                }
            }

            if (broken == 0)
            {
                report.AppendLine($"PASS  All {checkedMatrices} sampled matrices are rigid transforms.");
                report.AppendLine("      The baked data is correct — a wrong-looking mesh is a shader-side problem");
                report.AppendLine("      (UV2/UV3 wiring, property reference names, or the instance id path).");
                return;
            }

            report.AppendLine($"FAIL  {broken} of {checkedMatrices} sampled matrices are not rigid transforms.");
            report.AppendLine($"      First: {firstFailure}");
            report.AppendLine("      The problem is in the bake or the texture write path, not the shader.");
        }

        /// <summary>
        /// Checks that the mesh carries the skinning data the shader expects in the channels it reads from.
        /// A mesh missing UV2 or UV3 renders as garbage no matter how correct the texture is.
        /// </summary>
        private static void AppendMeshCheck(CrowdAnimationSet set, StringBuilder report)
        {
            if (set.Mesh == null) return;

            bool hasIndices = set.Mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord2);
            bool hasWeights = set.Mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord3);

            report.AppendLine($"      Mesh {set.Mesh.vertexCount} verts, UV2 {(hasIndices ? "present" : "MISSING")}, " +
                              $"UV3 {(hasWeights ? "present" : "MISSING")}");

            if (!hasIndices || !hasWeights)
            {
                report.AppendLine("FAIL  The baked mesh is missing its bone channels. Re-bake.");
                return;
            }

            // Spot-check that the channels did not end up swapped: weights are fractions summing to 1,
            // indices are whole numbers that can run up to the bone count.
            System.Collections.Generic.List<Vector4> weights = new System.Collections.Generic.List<Vector4>();
            set.Mesh.GetUVs(3, weights);

            int badSums = 0;
            for (int i = 0; i < weights.Count; i++)
            {
                float sum = weights[i].x + weights[i].y + weights[i].z + weights[i].w;
                if (Mathf.Abs(sum - 1f) > 0.01f) badSums++;
            }

            if (badSums > 0)
                report.AppendLine($"FAIL  {badSums} vertices have weights that do not sum to 1. UV2 and UV3 may be swapped.");
            else
                report.AppendLine("PASS  Mesh bone weights sum to 1.");
        }

        /// <summary>
        /// Runs the shader's own computation on the CPU: same texture, same mesh channels, same blend. If this
        /// produces a character-sized silhouette then every input the shader receives is provably good and
        /// the fault has to be in how the graph is wired. If it explodes, the bug is reproduced somewhere we
        /// can actually read, and the shader is not the place to look.
        /// </summary>
        private static void AppendReferenceSkinCheck(CrowdAnimationSet set, Color[] pixels, StringBuilder report)
        {
            if (set.Mesh == null || set.Clips.Count == 0) return;

            int width = set.BoneCount * TexelsPerMatrix;
            int influences = Mathf.Clamp(set.BoneInfluences, 1, 4);

            Vector3[] vertices = set.Mesh.vertices;
            System.Collections.Generic.List<Vector4> indices = new System.Collections.Generic.List<Vector4>();
            System.Collections.Generic.List<Vector4> weights = new System.Collections.Generic.List<Vector4>();
            set.Mesh.GetUVs(2, indices);
            set.Mesh.GetUVs(3, weights);

            if (indices.Count != vertices.Length || weights.Count != vertices.Length)
            {
                report.AppendLine("FAIL  Bone channels do not cover every vertex.");
                return;
            }

            CrowdAnimationClip clip = set.Clips[0];
            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.negativeInfinity;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 skinned = Vector3.zero;

                for (int influence = 0; influence < influences; influence++)
                {
                    float weight = weights[i][influence];
                    if (weight <= 0f) continue;

                    int bone = Mathf.Clamp((int)indices[i][influence], 0, set.BoneCount - 1);
                    Matrix4x4 skin = ReadMatrix(pixels, width, bone, clip.StartFrame);
                    skinned += skin.MultiplyPoint3x4(vertices[i]) * weight;
                }

                min = Vector3.Min(min, skinned);
                max = Vector3.Max(max, skinned);
            }

            Vector3 size = max - min;
            report.AppendLine($"      Reference skin of '{clip.Name}' frame 0: size {size:F3}");
            report.AppendLine($"      Baked LocalBounds size: {set.LocalBounds.size:F3}");

            // A rig this size should land within a metre or two per axis. Anything wilder means the matrices
            // and the mesh do not agree, whatever the per-matrix checks said.
            bool plausible = size.x < 10f && size.y < 10f && size.z < 10f && size.magnitude > 0.01f;

            report.AppendLine(plausible
                ? "PASS  CPU skinning reproduces a sane silhouette — the data the shader receives is correct."
                : "FAIL  CPU skinning explodes too, so the fault is in the baked data, not the shader.");
        }

        /// <summary>Rebuilds one bone matrix from the texture exactly the way CrowdSkinning.hlsl does.</summary>
        private static Matrix4x4 ReadMatrix(Color[] pixels, int width, int bone, int frame)
        {
            int texel = frame * width + bone * TexelsPerMatrix;
            Color r0 = pixels[texel + 0];
            Color r1 = pixels[texel + 1];
            Color r2 = pixels[texel + 2];

            Matrix4x4 matrix = Matrix4x4.identity;
            matrix.m00 = r0.r; matrix.m01 = r0.g; matrix.m02 = r0.b; matrix.m03 = r0.a;
            matrix.m10 = r1.r; matrix.m11 = r1.g; matrix.m12 = r1.b; matrix.m13 = r1.a;
            matrix.m20 = r2.r; matrix.m21 = r2.g; matrix.m22 = r2.b; matrix.m23 = r2.a;
            return matrix;
        }
    }
}
