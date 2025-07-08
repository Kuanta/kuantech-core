using Kuantech.Core.Database;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Kuantech.Editor.Database
{
    public class ScriptableDatabaseTableEditor : EditorWindow
    {
        [MenuItem("Kuantech/Database Table Editor")]
        private static void OpenWindow()
        {
            GetWindow<ScriptableDatabaseTableEditor>().Show();
        }

        private DataTable table;

        private Vector2 scroll;

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            // Table asset seçimi
            table = (DataTable)EditorGUILayout.ObjectField("Database Table", table, typeof(DataTable), false);

            if (table == null)
            {
                EditorGUILayout.HelpBox("Lütfen bir ScriptableObjectDatabaseTable seçin.", MessageType.Info);
                return;
            }

            var schema = table.Schema;
            var rows = table.Rows;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField($"Table: {table.TableName}", EditorStyles.boldLabel);

            scroll = EditorGUILayout.BeginScrollView(scroll);

            // Header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("ID", GUILayout.Width(100));
            foreach (var column in schema)
            {
                EditorGUILayout.LabelField(column.Name, GUILayout.Width(100));
            }
            EditorGUILayout.EndHorizontal();

            // Rows
            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];

                EditorGUILayout.BeginHorizontal();

                row.Id = EditorGUILayout.TextField(row.Id, GUILayout.Width(100));

                for (int i = 0; i < schema.Count; i++)
                {
                    if (i >= row.Values.Count)
                    {
                        Debug.LogWarning($"Row {row.Id} is missing column value at index {i}");
                        continue;
                    }

                    var data = row.Values[i];
                    DrawKtDataCell(data);
                }

                if (GUILayout.Button("✕", GUILayout.Width(25)))
                {
                    Undo.RecordObject(table, "Remove Row");
                    table.Rows.RemoveAt(rowIndex);
                    EditorUtility.SetDirty(table);
                    AssetDatabase.SaveAssets();
                    Repaint();
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Add Row", GUILayout.Width(100)))
            {
                Undo.RecordObject(table, "Add New Row");
                string newId = $"Row_{table.Rows.Count}";
                table.AddNewRow(newId);
                EditorUtility.SetDirty(table);
                AssetDatabase.SaveAssets();
                Repaint();
            }

            if (GUILayout.Button("Rebuild Lookup", GUILayout.Width(120)))
            {
                table.BuildTable();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawKtDataCell(DataTable.CellData data)
        {
            if (data == null)
            {
                EditorGUILayout.LabelField("null", GUILayout.Width(100));
                return;
            }

            switch (data.Value)
            {
                case KtFloat f:
                    f.Value = EditorGUILayout.FloatField(f.Value, GUILayout.Width(100));
                    break;
                case KtInt i:
                    i.Value = EditorGUILayout.IntField(i.Value, GUILayout.Width(100));
                    break;
                case KtString s:
                    s.Value = EditorGUILayout.TextField(s.Value, GUILayout.Width(100));
                    break;
                case KtBool b:
                    b.Value = EditorGUILayout.Toggle(b.Value, GUILayout.Width(100));
                    break;
                default:
                    EditorGUILayout.LabelField("?", GUILayout.Width(100));
                    break;
            }
        }
    }
    
    [CustomEditor(typeof(DataTable))]
    public class ScriptableObjectDatabaseTableEditorInspector : OdinEditor
    {
        public override void OnInspectorGUI()
        {
            // Varsayılan Inspector
            base.OnInspectorGUI();
    
            GUILayout.Space(10);
    
            if (GUILayout.Button("📝 Edit in Table Window", GUILayout.Height(30)))
            {
                var table = (DataTable)target;
                OpenEditor(table);
            }
        }
    
        private void OpenEditor(DataTable table)
        {
            var window = ScriptableDatabaseTableEditor.GetWindow<ScriptableDatabaseTableEditor>();
            window.titleContent = new GUIContent("Table Editor");
            
            // Internal set için reflection ya da property olabilir
            var tableField = typeof(ScriptableDatabaseTableEditor)
                .GetField("table", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    
            tableField?.SetValue(window, table);
        }
    }
}
