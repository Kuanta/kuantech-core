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
                Vector3 pointOnGrid = GetPointOnPlane();
                Vector3 botLeftPoint = GridBoard.transform.position - new Vector3(GridBoard.GetWidth()*0.5f, 0, GridBoard.GetDepth() * 0.5f);
                Vector3 diff = pointOnGrid - botLeftPoint;

                float horDist = Utils.Helpers.DotProjection(diff, GridBoard.transform.right);
                float depthDist = Utils.Helpers.DotProjection(diff, GridBoard.transform.forward);

                int col = Mathf.FloorToInt(horDist/GridBoard.CellWidth);
                int row = Mathf.FloorToInt(depthDist/GridBoard.CellHeight);

                // Consume the event to prevent other actions
                Event.current.Use();
                
                switch(CurrentMode)
                {
                    case EditorMode.Draw:
                        PaintTile(row, col);
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
            GameObject newTileObject = Instantiate(CurrentlySelectedTile);
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
        #endif

        public Vector3 GetPointOnPlane()
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            float groundY = GridBoard.transform.position.y;
            float rayDistance;
            Plane groundPlane = new Plane(Vector3.up, new Vector3(GridBoard.transform.position.x, groundY, GridBoard.transform.position.z));
            if (groundPlane.Raycast(ray, out rayDistance))
            {
                return ray.GetPoint(rayDistance);
            }
            return Vector3.zero;
        }
    }
}