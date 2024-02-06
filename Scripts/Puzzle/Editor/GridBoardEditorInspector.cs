using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Kuantech.Puzzle
{
    [CustomEditor(typeof(GridBoardEditor))]
    public class GridBoardEditorInspector : UnityEditor.Editor
    {
        private GridBoardEditor _gridBoardEditor;

        //Icons
        private GUIContent handIcon;
        private GUIContent tilePainterIcon;
        private GUIContent tileEraserIcon;

        private SerializedProperty tileLibraryProperty;
        private ReorderableList tileLibraryList;
        
        private void OnEnable()
        {
            _gridBoardEditor = (GridBoardEditor)target;
            handIcon = EditorGUIUtility.IconContent("d_Grid.Default");
            tilePainterIcon = EditorGUIUtility.IconContent("Toolbar Plus");
            tileEraserIcon = EditorGUIUtility.IconContent("d_TreeEditor.Trash");

            tileLibraryProperty = serializedObject.FindProperty("TileLibrary");
            if(tileLibraryProperty == null) return;
            tileLibraryList = new ReorderableList(serializedObject, tileLibraryProperty, true, true, true, true);
            if(tileLibraryList == null) return;

        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            serializedObject.Update();
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Update Editor Tiles"))
            {
                _gridBoardEditor.UpdateEditorTiles();
            }
            GUILayout.EndHorizontal();
            
            EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();

            if(GUILayout.Button(handIcon))
            {
                _gridBoardEditor.SetMode(GridBoardEditor.EditorMode.None);
            }

            if(GUILayout.Button(tilePainterIcon))
            {
                _gridBoardEditor.SetMode(GridBoardEditor.EditorMode.Draw);
            }

            if(GUILayout.Button(tileEraserIcon))
            {
                _gridBoardEditor.SetMode(GridBoardEditor.EditorMode.Delete);
            }
            GUILayout.EndHorizontal();


            EditorGUILayout.Space();
            
            DropAreaGUI();

            GUILayout.BeginHorizontal();
            if(GUILayout.Button("Update Editor Tiles"))
            {
                _gridBoardEditor.UpdateEditorTiles();
            }
            if(GUILayout.Button("Clear Tile Library"))
            {
                _gridBoardEditor.TileLibrary.Clear();
            }
            GUILayout.EndHorizontal();
            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI() {
            if(_gridBoardEditor == null) return;
            if(Selection.activeGameObject == null || _gridBoardEditor.CurrentMode != GridBoardEditor.EditorMode.None)
            {
                Selection.activeGameObject = _gridBoardEditor.GridBoard.gameObject;
            }
            _gridBoardEditor.HandleEditorUpdate();    
        }
        public void DropAreaGUI ()
        {
            Event evt = Event.current;
            Rect dropArea = GUILayoutUtility.GetRect (0.0f, 200.0f, GUILayout.ExpandWidth (true));
            GUI.Box (dropArea, "Tiles");
        
            switch (evt.type) {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains (evt.mousePosition))
                    return;
                
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            
                if (evt.type == EventType.DragPerform) {
                
                    DragAndDrop.AcceptDrag();

                    foreach (Object draggedObject in DragAndDrop.objectReferences)
                    {
                        if (draggedObject is GameObject prefab)
                        {
                            // Handle the prefab, add it to the list
                            AddPrefabToList(prefab);
                        }
                    }
                }
                evt.Use();
                break;
            }
            DisplayDroppedPrefabs(dropArea);
        }

        private void AddPrefabToList(GameObject prefab)
        {
            // Ensure that the prefab is not already in the list
            if (!ContainsPrefab(prefab.gameObject))
            {
                SerializedProperty prefabArray = tileLibraryList.serializedProperty;
                prefabArray.arraySize++;
                prefabArray.GetArrayElementAtIndex(prefabArray.arraySize - 1).objectReferenceValue = prefab;
                serializedObject.ApplyModifiedProperties();
            }
        }

        private bool ContainsPrefab(GameObject prefab)
        {
            SerializedProperty prefabArray = tileLibraryList.serializedProperty;
            for (int i = 0; i < prefabArray.arraySize; i++)
            {
                if (prefabArray.GetArrayElementAtIndex(i).objectReferenceValue == prefab)
                {
                    return true;
                }
            }
            return false;
        }

        private float TileLibraryButtonSize = 80.0f;
        private float TileButtonPadding = 10.0f;
        private void DisplayDroppedPrefabs(Rect dropArea)
        {
            int numColumns = Mathf.FloorToInt(dropArea.width / TileLibraryButtonSize);
            int numRows = Mathf.CeilToInt(tileLibraryList.count / (float)numColumns);
            GUILayout.BeginVertical();
            for (int row = 0; row < numRows; row++)
            {
                for (int col = 0; col < numColumns; col++)
                {
                    int index = row * numColumns + col;

                    if (index < tileLibraryList.count)
                    {
                        GameObject prefab = (GameObject)tileLibraryList.serializedProperty.GetArrayElementAtIndex(index).objectReferenceValue;

                        if (prefab != null)
                        {
                            // Display the prefab preview and name
                            Texture2D previewTexture = AssetPreview.GetAssetPreview(prefab);

                            Rect prefabRect = new Rect(TileButtonPadding + dropArea.x + col * (TileLibraryButtonSize+TileButtonPadding),
                            TileButtonPadding + dropArea.y + row * (TileLibraryButtonSize+TileButtonPadding), 
                            TileLibraryButtonSize, TileLibraryButtonSize);

                            bool isSelected = prefab == _gridBoardEditor.CurrentlySelectedTile;
                            if (isSelected)
                            {
                                EditorGUI.DrawRect(new Rect(prefabRect.x - 2, prefabRect.y - 2, prefabRect.width + 4, prefabRect.height + 4), Color.cyan);
                            }

                            if (GUI.Button(prefabRect, new GUIContent(previewTexture, prefab.name), GUIStyle.none))
                            {
                                _gridBoardEditor.CurrentlySelectedTile = prefab;
                                // Handle the button click (e.g., set the active tile)
                                Debug.LogError("Selected:"+prefab.name);
                            }

                            float labelHeight = 20f;
                            Rect labelRect = new Rect(prefabRect.x, prefabRect.y + prefabRect.height, prefabRect.width, labelHeight);
                            GUI.Label(labelRect, prefab.name, EditorStyles.centeredGreyMiniLabel);
                        }
                    }
                }
            }
            GUILayout.EndVertical();
        }
    }
}