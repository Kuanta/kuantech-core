using System.Collections.Generic;
using Kuantech.AI;
using UnityEditor;
using UnityEngine;

namespace Kuantech.Editor
{
    public class BehaviorTreeEditorManager : EditorWindow
    {
        private List<BehaviorTreeEditor> editorWindows = new List<BehaviorTreeEditor>();

        [MenuItem("Window/Behavior Tree Editor Manager")]
        public static void ShowWindow()
        {
            GetWindow<BehaviorTreeEditorManager>("Behavior Tree Editor Manager");
        }

        private void OnGUI()
        {
            GUILayout.Label("Manage Behavior Tree Editors", EditorStyles.boldLabel);

            if (GUILayout.Button("Open Behavior Tree Editor"))
            {
                OpenBehaviorTreeEditor();
            }
        }

        private void OpenBehaviorTreeEditor()
        {
            if (Selection.activeObject == null) return;
            BehaviourTreeBlueprint blueprint = Selection.activeObject as BehaviourTreeBlueprint;
            BehaviorTreeEditor editor = CreateInstance<BehaviorTreeEditor>();
            editor.DataToLoad = (BehaviourTreeBlueprint)Selection.activeObject;
            editor.Show();
            editor.LoadGraph();
            editorWindows.Add(editor);
        }
    }

}
