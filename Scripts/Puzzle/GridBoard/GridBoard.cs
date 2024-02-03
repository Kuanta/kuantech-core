using System;
using System.Collections.Generic;
using Kuantech.Core.FX;
using Sirenix.OdinInspector;
using Unity.Mathematics;
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
        [Header("Board Size")]
        public int RowCount = 5;
        public int ColumnCount = 5;
        [Header("Cell Size")]
        public float CellWidth = 1f;
        public float CellHeight = 1f;

        public GridTile[,] Tiles;
        [HideInInspector] public List<ExistingTileInfo> ExistingTiles = new List<ExistingTileInfo>();

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
        
        /// <summary>
        /// C
        /// </summary>
        protected void SetExistingTiles()
        {
            //Load existing tiles
            if (ExistingTiles == null) return;
            foreach (var existingTileInfo in ExistingTiles)
            {
                if (existingTileInfo.Prefab == null) continue;

                if (IsTileOccupied(existingTileInfo.Row, existingTileInfo.Col))
                {
                    continue;
                }
                CreateExistingTile(existingTileInfo);
            }
        }

        public virtual GridTile CreateExistingTile(ExistingTileInfo existingTileInfo)
        {
            GridTile tile = Instantiate(existingTileInfo.Prefab).GetComponent<GridTile>();
            SetTile(tile, existingTileInfo.Row, existingTileInfo.Col);
            tile.Spawn();
            return tile;
        }
        #region Move
        public virtual bool MoveTile(GridTile gridTile, int row, int col)
        {
            if(!IsCoordinateValid(row, col)) return false;

            if(IsTileOccupied(row, col)) return false;
            Tiles[gridTile.Row, gridTile.Column] = null;
            SetTile(gridTile, row, col);
            gridTile.transform.localPosition = GetLocalPosition(row, col);
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

        /// <summary>
        /// Gets the tile at given row col
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public GridTile GetTile(int row, int col)
        {
            if (!IsCoordinateValid(row, col)) return null;
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
                    }
                }
            }
            return emptyTileCoords;
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
            Vector3 botLeftPoint = transform.position - new Vector3(GetWidth() * 0.5f, 0, GetDepth() * 0.5f);
            Vector3 diff = pointOnGrid - botLeftPoint;

            float horDist = Utils.Helpers.DotProjection(diff, transform.right);
            float depthDist = Utils.Helpers.DotProjection(diff, transform.forward);

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
            float groundY = transform.position.y;
            float rayDistance;
            Plane groundPlane = new Plane(Vector3.up, new Vector3(transform.position.x, groundY, transform.position.z));
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
            return new Vector3(col * CellWidth - CellWidth * ColumnCount * 0.5f + CellWidth * 0.5f,
                                0,
                                row * CellHeight - CellHeight * RowCount * 0.5f + CellHeight * 0.5f);
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

        #region Effects
        public void PlayEffect(Effect effect, int row, int col)
        {
            //Boom
        }
        #endregion
    }
}