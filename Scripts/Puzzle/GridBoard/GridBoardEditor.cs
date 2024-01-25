using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Kuantech.Puzzle
{
    [ExecuteInEditMode]
    public class GridBoardEditor : MonoBehaviour {
        public enum EditorMode
        {
            None,
            Draw,
            Delete,
        }
        [Header("GridBoard")]
        public GridBoard GridBoard;
        public Transform GroupParent;
        
        [HideInInspector] public EditorMode CurrentMode = EditorMode.None;
        [HideInInspector] public List<GameObject> TileLibrary = new List<GameObject>();
        [HideInInspector] public GameObject CurrentlySelectedTile = null;
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
                GridBoard.GetRowColFromPointOnBoard(pointOnGrid, out int row, out int col);
                
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
                    default:
                        break;
                }
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

            Vector3 startPoint = transform.position - new Vector3((colCount - 1) * cellSize / 2, 0, (rowCount - 1) * cellSize / 2);

            for (int row = 0; row < rowCount; row++)
            {
                for (int col = 0; col < colCount; col++)
                {
                    Vector3 currentPoint = startPoint + new Vector3(col * cellSize, 0, row * cellSize);

                    // Draw horizontal lines
                    if (col < colCount - 1)
                    {
                        Vector3 nextPointH = currentPoint + new Vector3(cellSize, 0, 0);
                        Gizmos.DrawLine(currentPoint, nextPointH);
                    }

                    // Draw vertical lines
                    if (row < rowCount - 1)
                    {
                        Vector3 nextPointV = currentPoint + new Vector3(0, 0, cellSize);
                        Gizmos.DrawLine(currentPoint, nextPointV);
                    }
                }
            }
        }

        public void PaintTile(int row, int col)
        {
            if(CurrentMode != EditorMode.Draw || CurrentlySelectedTile == null) return;
            if(GridBoard.ExistingTiles == null) GridBoard.ExistingTiles = new List<GridTile>();
            GameObject newTileObject = PrefabUtility.InstantiatePrefab(CurrentlySelectedTile) as GameObject;
            GridTile tile = newTileObject.GetComponent<GridTile>();
            tile.Row = row;
            tile.Column = col;
            GridBoard.ExistingTiles.Add(tile);
            if(tile == null) 
            {
                Destroy(newTileObject);
                return;
            }
            tile.transform.position = GridBoard.transform.TransformPoint(GridBoard.GetLocalPosition(row, col));
            tile.transform.SetParent(GroupParent != null ? GroupParent : GridBoard.transform);
            tile.transform.localRotation = Quaternion.identity;
        }

        public void DeleteTile(int row, int col)
        {
            if (CurrentMode != EditorMode.Delete) return;
            HashSet<GridTile> tilesToDelete = new HashSet<GridTile>();
            for(int i=0;i<GridBoard.ExistingTiles.Count;++i)
            {
                GridTile tile = GridBoard.ExistingTiles[i];
                if(tile == null) continue;
                if(tile.Row == row && tile.Column == col)
                {
                    tilesToDelete.Add(tile);
                }
            }

            foreach(var tile in tilesToDelete)
            {
                GridBoard.ExistingTiles.Remove(tile);
                DestroyImmediate(tile.gameObject);
            }
        }
        #endif

    }
}