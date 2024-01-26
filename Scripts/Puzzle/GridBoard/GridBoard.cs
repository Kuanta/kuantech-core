using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Puzzle
{
    public class GridBoard : MonoBehaviour
    {
        [Header("Board Size")]
        public int RowCount = 5;
        public int ColumnCount = 5;
        [Header("Cell Size")]
        public float CellWidth = 1f;
        public float CellHeight = 1f;

        public GridTile[,] Tiles;
        [HideInInspector] public List<GridTile> ExistingTiles = new List<GridTile>();

        public delegate void TileOperation(GridTile tile);
        public void CreateBoard()
        {
            Tiles = new GridTile[RowCount, ColumnCount];
            for (int r = 0; r < RowCount; ++r)
            {
                for (int c = 0; c < ColumnCount; ++c)
                {
                    Tiles[r,c] = null;
                }
            }
            
            //Load existing tiles
            if(ExistingTiles == null) return;
            foreach(var existingTile in ExistingTiles)
            {
                if(existingTile == null) continue;
                if(IsTileOccupied(existingTile.Row, existingTile.Column))
                {
                    Destroy(existingTile.gameObject);
                    continue;
                }
                SetTile(existingTile, existingTile.Row, existingTile.Column);
                existingTile.Spawn();
            }
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
        public void SetTile(GridTile gridTile, int row, int col)
        {
            if (!IsCoordinateValid(row, col)) return;
            Tiles[row, col] = gridTile;
            gridTile.SetRowCol(row, col);
        }

        public GridTile GetTile(int row, int col)
        {
            if (!IsCoordinateValid(row, col)) return null;
            return Tiles[row, col];
        }
        
        public bool IsTileOccupied(int row, int col)
        {
            if(!IsCoordinateValid(row, col)) return false;
            return Tiles[row, col] != null;
        }
        #endregion



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

        public float GetWidth()
        {
            return (ColumnCount) * CellWidth;
        }

        public float GetDepth()
        {
            return (RowCount) * CellHeight;
        }
        #endregion
    }
}