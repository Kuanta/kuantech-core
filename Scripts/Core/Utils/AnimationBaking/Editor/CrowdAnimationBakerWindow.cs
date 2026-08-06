using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Kuantech.Core.Utils
{
    /// <summary>
    /// Authoring front end for <see cref="CrowdAnimationBaker"/>. Its main job beyond collecting settings is
    /// showing what the bake will cost before it runs — texture size grows with bones times frames, and it is
    /// far easier to notice a runaway frame rate here than after waiting for the bake.
    /// </summary>
    public class CrowdAnimationBakerWindow : EditorWindow
    {
        private const int TexelsPerMatrix = 3;

        private GameObject _source;
        private RuntimeAnimatorController _controller;
        private readonly List<AnimationClip> _clips = new List<AnimationClip>();

        private int _frameRate = 30;
        private int _boneInfluences = 4;
        private bool _highPrecision;

        private string _outputFolder = "Assets";
        private string _assetName = "CrowdAnimationSet";

        private Vector2 _scroll;
        private string _status;
        private MessageType _statusType = MessageType.Info;

        [MenuItem("Kuantech/Animation Baking/Crowd Animation Baker")]
        public static void ShowWindow()
        {
            GetWindow<CrowdAnimationBakerWindow>("Crowd Baker").minSize = new Vector2(380f, 460f);
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawSource();
            EditorGUILayout.Space();
            DrawClips();
            EditorGUILayout.Space();
            DrawSettings();
            EditorGUILayout.Space();
            DrawOutput();
            EditorGUILayout.Space();
            DrawEstimate();
            EditorGUILayout.Space();
            DrawBakeButton();

            if (!string.IsNullOrEmpty(_status)) EditorGUILayout.HelpBox(_status, _statusType);

            EditorGUILayout.EndScrollView();
        }

        private void DrawSource()
        {
            EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _source = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("Character", "Prefab with the Animator and SkinnedMeshRenderer to bake."),
                _source, typeof(GameObject), true);

            // Offer the character's own controller as the obvious clip source.
            if (EditorGUI.EndChangeCheck() && _source != null && _controller == null)
            {
                Animator animator = _source.GetComponentInChildren<Animator>();
                if (animator != null) _controller = animator.runtimeAnimatorController;
            }

            if (_source == null) return;

            if (_source.GetComponentInChildren<SkinnedMeshRenderer>() == null)
                EditorGUILayout.HelpBox("No SkinnedMeshRenderer found on this object.", MessageType.Error);
            if (_source.GetComponentInChildren<Animator>() == null)
                EditorGUILayout.HelpBox("No Animator found. Sampling needs one, Humanoid rigs especially.", MessageType.Error);
        }

        private void DrawClips()
        {
            EditorGUILayout.LabelField("Clips", EditorStyles.boldLabel);

            _controller = (RuntimeAnimatorController)EditorGUILayout.ObjectField(
                new GUIContent("Controller", "Optional — only used to fill the clip list below."),
                _controller, typeof(RuntimeAnimatorController), false);

            using (new EditorGUI.DisabledScope(_controller == null))
            {
                if (GUILayout.Button("Load Clips From Controller"))
                {
                    _clips.Clear();
                    _clips.AddRange(CrowdAnimationBaker.CollectClips(_controller));
                    SetStatus($"Loaded {_clips.Count} clip(s).", MessageType.Info);
                }
            }

            for (int i = 0; i < _clips.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                _clips[i] = (AnimationClip)EditorGUILayout.ObjectField(_clips[i], typeof(AnimationClip), false);

                if (GUILayout.Button("-", GUILayout.Width(24f)))
                {
                    _clips.RemoveAt(i);
                    EditorGUILayout.EndHorizontal();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add Clip")) _clips.Add(null);
        }

        private void DrawSettings()
        {
            EditorGUILayout.LabelField("Bake Settings", EditorStyles.boldLabel);

            _frameRate = EditorGUILayout.IntSlider(
                new GUIContent("Frame Rate", "Sampling rate. The shader interpolates between rows, so 30 is usually enough."),
                _frameRate, 10, 60);

            _boneInfluences = EditorGUILayout.IntPopup(
                new GUIContent("Bone Influences", "Bones per vertex. 2 halves the shader's texture reads — good for mobile."),
                _boneInfluences, new[] { new GUIContent("2 (mobile)"), new GUIContent("4 (quality)") }, new[] { 2, 4 });

            _highPrecision = EditorGUILayout.Toggle(
                new GUIContent("High Precision", "Store matrices as full floats instead of halves. Doubles the texture size."),
                _highPrecision);

            EditorGUILayout.HelpBox(
                _boneInfluences == 2
                    ? "Use the CrowdSkin2 function on the Custom Function node."
                    : "Use the CrowdSkin4 function on the Custom Function node.",
                MessageType.None);
        }

        private void DrawOutput()
        {
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            _outputFolder = EditorGUILayout.TextField("Folder", _outputFolder);
            if (GUILayout.Button("...", GUILayout.Width(28f)))
            {
                string picked = EditorUtility.OpenFolderPanel("Output Folder", _outputFolder, string.Empty);
                if (!string.IsNullOrEmpty(picked) && picked.StartsWith(Application.dataPath))
                    _outputFolder = "Assets" + picked.Substring(Application.dataPath.Length);
            }
            EditorGUILayout.EndHorizontal();

            _assetName = EditorGUILayout.TextField("Asset Name", _assetName);
            EditorGUILayout.HelpBox("Re-baking over an existing set keeps its material assignment and every reference to it.",
                MessageType.None);
        }

        /// <summary>
        /// Shows the texture the current settings would produce. Bones and frames both multiply into it, so
        /// this is the number to watch when adding clips or raising the frame rate.
        /// </summary>
        private void DrawEstimate()
        {
            if (_source == null || _clips.Count == 0) return;

            SkinnedMeshRenderer skinnedMesh = _source.GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinnedMesh == null || skinnedMesh.bones == null || skinnedMesh.bones.Length == 0) return;

            int boneCount = skinnedMesh.bones.Length;
            int totalFrames = 0;
            foreach (AnimationClip clip in _clips)
            {
                if (clip != null) totalFrames += Mathf.Max(1, Mathf.CeilToInt(clip.length * _frameRate));
            }
            if (totalFrames == 0) return;

            int width = boneCount * TexelsPerMatrix;
            int bytesPerTexel = _highPrecision ? 16 : 8;
            float kilobytes = width * totalFrames * bytesPerTexel / 1024f;

            EditorGUILayout.LabelField("Estimate", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Bones: {boneCount}   Frames: {totalFrames}");
            EditorGUILayout.LabelField($"Texture: {width} x {totalFrames}   ({kilobytes:0.#} KB)");

            if (width > SystemInfo.maxTextureSize || totalFrames > SystemInfo.maxTextureSize)
            {
                EditorGUILayout.HelpBox($"Over this platform's {SystemInfo.maxTextureSize} texture limit. " +
                                        "Lower the frame rate or split the clips across two sets.", MessageType.Error);
            }
        }

        private void DrawBakeButton()
        {
            bool ready = _source != null && _clips.Count > 0;

            EditorGUI.BeginDisabledGroup(!ready);
            bool pressed = GUILayout.Button("Bake", GUILayout.Height(30f));
            EditorGUI.EndDisabledGroup();

            if (!pressed || !ready) return;

            CrowdAnimationBaker.BakeSettings settings = new CrowdAnimationBaker.BakeSettings
            {
                Source = _source,
                Clips = new List<AnimationClip>(_clips),
                FrameRate = _frameRate,
                BoneInfluences = _boneInfluences,
                HighPrecision = _highPrecision,
                OutputFolder = _outputFolder,
                AssetName = _assetName,
            };

            CrowdAnimationSet set = CrowdAnimationBaker.Bake(settings, out string error);

            if (set == null)
            {
                SetStatus(error, MessageType.Error);
                return;
            }

            Selection.activeObject = set;
            EditorGUIUtility.PingObject(set);
            SetStatus($"Baked {set.Clips.Count} clip(s), {set.TotalFrames} frames, {set.BoneCount} bones.\n" +
                      "Assign a material using a CrowdSkin shader to finish.", MessageType.Info);
        }

        private void SetStatus(string message, MessageType type)
        {
            _status = message;
            _statusType = type;
        }
    }
}
