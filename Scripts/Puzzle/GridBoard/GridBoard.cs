using System;
using System.Collections.Generic;
using Kuantech.Core.FX;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Kuantech.Puzzle
{
    [Serializable]
    public class ExistingTileInfo
    {
        public GameObject Prefab;
        public int Row;
        public int Col;
    }

    public class GridBoard : MonoBehaviour
    {
        public enum DirectionTypes : uint
        {
            Top = 0, Right = 1, Bottom = 2, Left = 3, TopLeft = 4, TopRight = 5, BottomLeft = 6, BottomRight = 7, Invalid
        }

        [Header("Board Size")]
        public int RowCount = 5;
        public int ColumnCount = 5;
        public Vector3 ForwardVector = Vector3.up;
        public Vector3 RightVector = Vector3.right;

        [Header("Cell Size")]
        public float CellWidth = 1f;
        public float CellHeight = 1f;

        public GridTile[,] Tiles;

        public delegate void TileOperation(GridTile tile);
        public virtual void CreateBoard()
        {
            Tiles = new GridTile[RowCount, ColumnCount];
            for (int r = 0; r < RowCount; ++r)
            {
                for (int c = 0; c < ColumnCount; ++c)
                {
                    Tiles[r,c] = null;
                }
            }

            SetExistingTiles();
        }
        
        public virtual void RestartBoard()
        {
            ClearBoard();
            SetExistingTiles();
        }
        
        /// <summary>
        /// C
        /// </summary>
        private GridBoardEditorTile[] _editorTiles;
        protected void SetExistingTiles()
        {
            //Load existing tiles
            GridBoardEditorTile[] editorTiles = GetComponentsInChildren<GridBoardEditorTile>();
            for(int i=0;i<editorTiles.Length;++i)
            {
                GridBoardEditorTile existingTileInfo = editorTiles[i];
                if (existingTileInfo.Prefab == null) continue;

                if (IsTileOccupied(existingTileInfo.Row, existingTileInfo.Column))
                {
                    continue;
                }
                CreateExistingTile(existingTileInfo);
                existingTileInfo.DestroyEditorGameobject();
            }
        }

        public virtual GridTile CreateExistingTile(GridBoardEditorTile existingTileInfo)
        {
            GridTile tile = Instantiate(existingTileInfo.EditorObject).GetComponent<GridTile>();
            tile.gameObject.SetActive(true);
            SetTile(tile, existingTileInfo.Row, existingTileInfo.Column);
            tile.Spawn();
            tile.OnCreateExisting();
            return tile;
        }
        #region Move
        public virtual bool MoveTile(GridTile gridTile, int row, int col, bool setPosition = true)
        {
            if(!IsCoordinateValid(row, col)) return false;

            if(IsTileOccupied(row, col)) return false;
            Tiles[row, col] = null;
            SetTile(gridTile, row, col, setPosition);
            return true;
        }
        #endregion

        #region Query Methods
        /// <summary>
        /// Sets the tile for a grid tile
        /// </summary>
        /// <param name="gridTile">Grid Tile to set</param>
        /// <param name="row">Desired row</param>
        /// <param name="col">Desired col</param>
        /// <param name="setPosition">If flag is set to true, the position will be set</param>
        public virtual void SetTile(GridTile gridTile, int row, int col, bool setPosition = true)
        {
            if (!IsCoordinateValid(row, col)) return;
            gridTile.ParentBoard = this;
            Tiles[row, col] = gridTile;
            gridTile.SetRowCol(row, col);
            if(setPosition)
            {
                gridTile.transform.SetParent(transform);
                gridTile.SetLocalPosition(GetLocalPosition(row, col));
                gridTile.transform.localRotation = Quaternion.identity;
                gridTile.transform.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// Unsets the tile at given row, col
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public GridTile UnsetTile(int row, int col)
        {
            GridTile existingTile = GetTile(row, col);
            Tiles[row, col] = null;
            return existingTile;
        }

        public void UnsetTiles(List<GridTile> tiles)
        {
            foreach(var tile in tiles)
            {
                UnsetTile(tile);
            }
        }
        public void UnsetTile(GridTile tile)
        {
            UnsetTile(tile.Row, tile.Column);
            tile.OnDespawn();
            Destroy(tile.gameObject);
        }
        
        /// <summary>
        /// Gets the tile at given row col
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public GridTile GetTile(int row, int col)
        {
            if (!IsCoordinateValid(row, col)) return null;
            if(Tiles == null)
            {
                Debug.LogError("Null");
                return null;
            }
            if(Tiles[row,col] != null && (Tiles[row,col].Row != row || Tiles[row,col].Column != col))
            {
                Debug.LogError("WE HAVE ROW COL MISMATCH!");
            }
            return Tiles[row, col];
        }
        
        public bool IsTileOccupied(int row, int col)
        {
            if(!IsCoordinateValid(row, col)) return false;
            return Tiles[row, col] != null;
        }

        public bool IsTileValidAndEmpty(int row, int col)
        {
            return !IsTileOccupied(row, col) && IsCoordinateValid(row, col);
        }
        public int GetEmptyTileCount()
        {
            int emptyCount = 0;
            for(int r=0;r<RowCount;++r)
            {
                for(int c=0;c<ColumnCount;++c)
                {
                    if(GetTile(r,c) == null) emptyCount++;
                }
            }
            return emptyCount;
        }

        /// <summary>
        /// Returns the first empty tile starting from R=0, C=0
        /// </summary>
        /// <returns>Vector2 in the form of (row, col) </returns>
        public Vector2Int GetEmptyRowCol()
        {
            Vector2Int emptyTileCoords = Vector2Int.one * -1; // Start as invalid
            for (int r = 0; r < RowCount; ++r)
            {
                for (int c = 0; c < ColumnCount; ++c)
                {
                    if (GetTile(r, c) == null)
                    {
                        emptyTileCoords.x = r;
                        emptyTileCoords.y = c;
                        return emptyTileCoords;
                    }
                }
            }
            return emptyTileCoords;
        }

        /// <summary>
        /// Gets a list of empty row columns
        /// </summary>
        /// <returns></returns>
        public List<Vector2Int> GetEmptyTiles()
        {
            List<Vector2Int> emptyTiles = new List<Vector2Int>();
            for (int r = 0; r < RowCount; ++r)
            {
                for (int c = 0; c < ColumnCount; ++c)
                {
                    if (IsTileOccupied(r,c))
                    {
                        continue;
                    }
                    emptyTiles.Add(new Vector2Int(r,c));
                }
            }
            return emptyTiles;
        }
        #endregion


        public void ClearBoard()
        {
            for (int r = 0; r < RowCount; ++r)
            {
                for (int c = 0; c < ColumnCount; ++c)
                {
                    GridTile tile = GetTile(r, c);
                    if(tile != null)
                    {
                        Destroy(tile.gameObject);
                    }
                    Tiles[r, c] = null;
                }
            }
        }
        #region Utility Methods
        /// <summary>
        /// Returns row and col 
        /// </summary>
        /// <param name="pointOnGrid"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        public void GetRowColFromPointOnBoard(Vector3 pointOnGrid, out int row, out int col)
        {
            Vector3 localBotLeft = -ForwardVector * GetDepth() * 0.5f - RightVector * GetWidth() * 0.5f;
            Vector3 botLeftPoint = transform.TransformPoint(localBotLeft);
            Vector3 diff = pointOnGrid - botLeftPoint;
            Vector3 localDiff = transform.InverseTransformDirection(diff);
            float horDist = Kuantech.Utils.Helpers.DotProjection(localDiff, RightVector);
            float depthDist = Kuantech.Utils.Helpers.DotProjection(localDiff, ForwardVector);
            col = Mathf.FloorToInt(horDist / CellWidth);
            row = Mathf.FloorToInt(depthDist / CellHeight);
        }

        public void ApplyOperationToTiles(TileOperation operation)
        {
            for(int r=0;r<RowCount;++r)
            {
                for(int c=0;c<ColumnCount;++c)
                {
                    if(Tiles[r,c] == null) continue;
                    operation.Invoke(Tiles[r,c]);
                }
            }
        }

        public bool IsCoordinateValid(int row, int col)
        {
            if(row < 0 || col < 0 || row >= RowCount || col >= ColumnCount) return false;
            return true;
        }

        /// <summary>
        /// Returns the flattened coordinates from row and col
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public int GetFlattenCoordinates(int row, int col)
        {
            return row*ColumnCount + col;
        }

        /// <summary>
        /// Returns the row and col indices from a flattened coordinate
        /// </summary>
        /// <param name="flat"></param>
        /// <returns>(row, col)</returns>
        public Vector2 GetRowColFromFlattened(int flat)
        {
            int rowCount = Mathf.FloorToInt(flat / ColumnCount);
            int colCount = flat - rowCount * ColumnCount;
            return new Vector2Int(rowCount, colCount);
        }

        public Vector3 GetPointOnPlane(Ray ray)
        {
            //todo: Fix this to comply with rotated boards
            float rayDistance;
            Vector3 groundPlaneNormal = Vector3.Cross(transform.rotation * ForwardVector, transform.rotation * RightVector).normalized;
            Plane groundPlane = new Plane(groundPlaneNormal, transform.position);
            if (groundPlane.Raycast(ray, out rayDistance))
            {
                return ray.GetPoint(rayDistance);
            }
            return Vector3.zero;
        }
        /// <summary>
        /// Returns the local position from row and col
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public Vector3 GetLocalPosition(int row, int col)
        {
            Vector3 horizontalPosition = RightVector * (col * CellWidth - CellWidth * ColumnCount * 0.5f + CellWidth * 0.5f);
            Vector3 depthPosition = ForwardVector * (row * CellHeight - CellHeight * RowCount * 0.5f + CellHeight * 0.5f);
            return horizontalPosition + depthPosition;
        }

        public Vector3 GetGlobalPosition(int row, int col)
        {
            Vector3 localPos = GetLocalPosition(row, col);
            return transform.TransformPoint(localPos);
        }
        public float GetWidth()
        {
            return (ColumnCount) * CellWidth;
        }

        public float GetDepth()
        {
            return (RowCount) * CellHeight;
        }

        #endregion

        #if UNITY_EDITOR
        [Button("Select Tile")]
        public void SelectTile(int row, int col)
        {
            GridTile tile = GetTile(row, col);
            if(tile == null)
            {
                Debug.LogError("Null tile!");
                return;
            }
            Selection.activeGameObject = tile.gameObject;
        }
        #endif
        
        #region Effects
        public void PlayEffect(Effect effect, int row, int col)
        {
            //Boom
        }
        #endregion
    }
}