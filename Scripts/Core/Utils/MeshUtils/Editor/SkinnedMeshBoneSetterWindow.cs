using UnityEngine;
using UnityEditor;
using Kuantech.Utils; 

namespace Kuantech.EditorTools
{
    public class SkinnedMeshBoneSetterWindow : EditorWindow
    {
        private SkinnedMeshRenderer _targetMesh;
        private Transform _rootActor;
        private string _rootBoneName = "Hips";

        [MenuItem("Kuantech/MeshUtils/Skinned Mesh Bone Setter")]
        public static void ShowWindow()
        {
            GetWindow<SkinnedMeshBoneSetterWindow>("Bone Setter");
        }

        private void OnGUI()
        {
            GUILayout.Label("Skinned Mesh Bone Remapper", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 1. Girdi Alanları
            _targetMesh = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Zırh (Skinned Mesh)", _targetMesh, typeof(SkinnedMeshRenderer), true);
            _rootActor = (Transform)EditorGUILayout.ObjectField("Ana Karakter (Root)", _rootActor, typeof(Transform), true);
            _rootBoneName = EditorGUILayout.TextField("Root Bone Name", _rootBoneName);

            EditorGUILayout.Space();

            if (_targetMesh == null || _rootActor == null)
            {
                EditorGUILayout.HelpBox("Lütfen zırhı ve ana karakterin kök objesini seçin.", MessageType.Info);
                GUI.enabled = false; // Butonu kilitle
            }

            if (GUILayout.Button("Kemikleri Eşleştir (Bind Bones)"))
            {
                BindBones();
            }
            
            GUI.enabled = true; // Kilidi aç
        }

        private void BindBones()
        {
            Undo.RecordObject(_targetMesh, "Bone Remap");
            MeshHelpers.SetSkinnedMeshBones(_targetMesh, _rootActor, _rootBoneName);
            EditorUtility.SetDirty(_targetMesh);

            Debug.Log($"<color=green>Başarılı:</color> {_targetMesh.name} zırhı, {_rootActor.name} iskeletine bağlandı!");
        }
    }
}