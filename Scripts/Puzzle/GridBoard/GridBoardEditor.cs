using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Kuantech.Puzzle
{
    #if UNITY_EDITOR
    [ExecuteInEditMode]
    public class GridBoardEditor : MonoBehaviour {
        public enum EditorMode
        {
            None,
            Draw,
            Delete,
            DeleteAndDraw,
        }

        [Header("GridBoard")]
        public GridBoard GridBoard;
        public Transform GroupParent;
        public GridTileLibrary TileCollection;
        public Color DebugLinesColor = Color.white;
        [HideInInspector] public EditorMode CurrentMode = EditorMode.None;
        [HideInInspector] public List<GameObject> TileLibrary = new List<GameObject>();
        [HideInInspector] public GameObject CurrentlySelectedTile = null;
        public List<GridBoardEditorTile> EditorTiles = new List<GridBoardEditorTile>();

        //Mode changes
        public void SetMode(GridBoardEditor.EditorMode mode)
        {
            CurrentMode = mode;
        }

        public void HandleEditorUpdate()
        {
            if (Application.isPlaying || Selection.activeGameObject != gameObject) return;
            Selection.activeGameObject = gameObject;
            if (Event.current != null && Event.current.isMouse && Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                if (GridBoard == null)
                {
                    GridBoard = GetComponent<GridBoard>();
                    if (GridBoard == null) return;
                }
                
                Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                Vector3 pointOnGrid = GridBoard.GetPointOnPlane(ray);
                GridTileCoordinate coord = GridBoard.GetRowColFromPointOnBoard(pointOnGrid);
                int row = coord.Row;
                int col = coord.Column;
                
                // Consume the event to prevent other actions
                Event.current.Use();
                
                switch(CurrentMode)
                {
                    case EditorMode.Draw:
                        PaintTile(row, col);
                        break;
                    case EditorMode.Delete:
                        DeleteTile(row, col);
                        break;
                    case EditorMode.DeleteAndDraw:
                        DeleteAndPaint(row, col);
                        break;
                    default:
                        break;
                }
            }
        }

        private void OnValidate() 
        {
            if(TileCollection != null)
            {
                TileLibrary = new List<GameObject>(TileCollection.Tiles);
            }

        }
        
        #if UNITY_EDITOR
       
        private void OnDrawGizmosSelected()
        {
            DrawGrid();
        }
        private void DrawGrid()
        {
            if(GridBoard == null) return;
            int colCount = GridBoard.ColumnCount+1;
            int rowCount = GridBoard.RowCount+1;
            float cellSize = GridBoard.CellHeight;
            Gizmos.color = Color.white;

            Vector3 startHorizontal = GridBoard.RightVector * ((colCount - 1) * cellSize / 2);
            Vector3 startDepth = GridBoard.ForwardVector * ((rowCount - 1) * cellSize / 2);
            Vector3 startPoint = -startHorizontal - startDepth;

            for (int row = 0; row < rowCount; row++)
            {
                for (int col = 0; col < colCount; col++)
                {
                    Vector3 currentPoint = startPoint + GridBoard.RightVector * (col * cellSize) + GridBoard.ForwardVector * (row * cellSize);
                    Vector3 globalCurrent = GridBoard.transform.TransformPoint(currentPoint);
                    Gizmos.color = DebugLinesColor;
                    // Draw horizontal lines
                    if (col < colCount - 1)
                    {
                        Vector3 nextPointH = currentPoint + GridBoard.RightVector * cellSize;
                        nextPointH = GridBoard.transform.TransformPoint(nextPointH);
                        Gizmos.DrawLine(globalCurrent, nextPointH);
                    }

                    // Draw vertical lines
                    if (row < rowCount - 1)
                    {
                        Vector3 nextPointV = currentPoint + GridBoard.ForwardVector * cellSize;
                        nextPointV = GridBoard.transform.TransformPoint(nextPointV);
                        Gizmos.DrawLine(globalCurrent, nextPointV);
                    }
                }
            }
        }

        public void PaintTile(int row, int col)
        {
            if(CurrentlySelectedTile == null || IsTileOccupied(row, col)) return;
       

            GameObject newTileObject = PrefabUtility.InstantiatePrefab(CurrentlySelectedTile) as GameObject;
            
            GameObject emptyTileObject = new GameObject($"EditorTile_{row}_{col}");
           
            GridBoardEditorTile editorTileComp = emptyTileObject.AddComponent<GridBoardEditorTile>();
          

            emptyTileObject.transform.SetParent(GroupParent != null ? GroupParent : GridBoard.transform);
            GridTile tile = newTileObject.GetComponent<GridTile>();
            tile.Row = row;
            tile.Column = col;
            editorTileComp.Prefab = CurrentlySelectedTile;
            editorTileComp.Row = row;
            editorTileComp.Column = col;
            editorTileComp.EditorObject = tile.gameObject;

            GridBoardEditorTile prefabEditorTileComp = newTileObject.GetComponent<GridBoardEditorTile>();
            if (prefabEditorTileComp != null)
            {
                editorTileComp.Prefab = prefabEditorTileComp.Prefab;
            }

            EditorTiles.Add(editorTileComp);

            if(tile == null) 
            {
                Destroy(newTileObject);
                return;
            }

            emptyTileObject.transform.position = GridBoard.transform.TransformPoint(GridBoard.GetLocalPosition(row, col));
            emptyTileObject.transform.localRotation = Quaternion.identity;
            tile.transform.SetParent(emptyTileObject.transform);
            tile.transform.localPosition = Vector3.zero;
            tile.transform.localRotation = Quaternion.identity;
            UpdateEditorTiles();
            EditorUtility.SetDirty(this);
        }

        public void DeleteAndPaint(int row, int col)
        {
            DeleteTile(row, col);
            PaintTile(row, col);
        }

        public bool IsTileOccupied(int row, int col)
        {
            foreach(var existingTileInfo in EditorTiles)
            {
                if(existingTileInfo.Row == row && existingTileInfo.Column == col) return true;
            }
            return false;
        }

        public GridBoardEditorTile GetEditorTile(int row, int col)
        {
            foreach(var tile in EditorTiles)
            {
                if(tile.Row == row && tile.Column == col) return tile;
            }
            return null;
        }

        /// <summary>
        /// Deletes a tile
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        public void DeleteTile(int row, int col)
        {
            HashSet<GridBoardEditorTile> tilesToDelete = new HashSet<GridBoardEditorTile>();
            for(int i=0;i<EditorTiles.Count;++i)
            {
                GridBoardEditorTile tileInfo = EditorTiles[i];
                if(tileInfo == null) continue;
                if(tileInfo.Row == row && tileInfo.Column == col)
                {
                    tilesToDelete.Add(tileInfo);
                    DestroyImmediate(tileInfo.gameObject);
                }
            }

            foreach(var tile in tilesToDelete)
            {
                EditorTiles.Remove(tile);
            }
            UpdateEditorTiles();
            EditorUtility.SetDirty(this);
        }

        public void DeleteAllTiles()
        {
            HashSet<GridBoardEditorTile> tilesToDelete = new HashSet<GridBoardEditorTile>();
            for (int i = 0; i < EditorTiles.Count; ++i)
            {
                GridBoardEditorTile tileInfo = EditorTiles[i];
                if (tileInfo == null) continue;
                tilesToDelete.Add(tileInfo);
                DestroyImmediate(tileInfo.gameObject);
            }
            foreach (var tile in tilesToDelete)
            {
                EditorTiles.Remove(tile);
            }
            UpdateEditorTiles();
            EditorUtility.SetDirty(this);
        }

        public void UpdateEditorTiles()
        {
            Transform parent = (GroupParent != null ? GroupParent : GridBoard.transform);
            EditorTiles = parent.GetComponentsInChildren<GridBoardEditorTile>().ToList();
        }
        #endif

    }
    #endif
}