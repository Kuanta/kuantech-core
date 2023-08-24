using Kuantech.AI;
using UnityEditor;
using UnityEngine;

namespace Kuantech.Editor
{
    [CustomEditor(typeof(BehaviourTreeBlueprint))]
    public class BtGraphSaveDataInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Open Behavior Tree Editor"))
            {
                BehaviorTreeEditor editor = (BehaviorTreeEditor)EditorWindow.GetWindow(typeof(BehaviorTreeEditor));
                editor.DataToLoad = (BehaviourTreeBlueprint)target;
                editor.Show();
                editor.LoadGraph();
            }
        }
    }
}