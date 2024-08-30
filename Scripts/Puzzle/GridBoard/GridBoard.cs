using System;
using System.Collections.Generic;
using Kuantech.Core.FX;
using Kuantech.Utils;
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

        [Header("Origin Offset")] 
        public Vector2 OriginOffset = new Vector2(-0.5f, -0.5f);

        [Header("BackgroundTile object")] 
        public GridTileBackground BackgroundGameObjectPrefab;
        
        public GridTile[,] Tiles;
        public GridTileBackground[,] BackgroundObjects;

        public delegate void TileOperation(GridTile tile);
        public virtual void CreateBoard()
        {
            Tiles = new GridTile[RowCount, ColumnCount];
            BackgroundObjects = new GridTileBackground[RowCount, ColumnCount];
            for (int r = 0; r < RowCount; ++r)
            {
                for (int c = 0; c < ColumnCount; ++c)
                {
                    Tiles[r,c] = null;
                    AddBackgroundObject(r,c);
                }
            }

            //SetExistingTiles(true);
            GridTile[] existingTiles = GetComponentsInChildren<GridTile>();
            foreach (var tile in existingTiles)
            {
                GridTileCoordinate coord = GetRowColFromPosition(tile.transform.position);
                SetTile(tile, coord.Row, coord.Column);
                tile.Spawn();
            }
        }

        public virtual void AddBackgroundObject(int row, int col)
        {
            if (BackgroundGameObjectPrefab != null)
            {
                GridTileBackground bgObj = Instantiate(BackgroundGameObjectPrefab);
                bgObj.transform.parent = transform;
                bgObj.transform.localPosition = GetLocalPosition(row, col);
                bgObj.transform.localRotation = Quaternion.identity;
                BackgroundObjects[row, col] = bgObj;
            }
        }
        public virtual void RestartBoard()
        {
            ClearBoard();
            //SetExistingTiles(true);
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
        public bool IsCoordinateValid(GridTileCoordinate coordinate)
        {
            return IsCoordinateValid(coordinate.Row, coordinate.Column);
        }
        
        public bool IsCoordinateValid(int row, int col)
        {
            if(row < 0 || col < 0 || row >= RowCount || col >= ColumnCount) return false;
            return true;
        }
        public bool IsTileValidAndEmpty(GridTileCoordinate coordinate)
        {
            return IsTileValidAndEmpty(coordinate.Row, coordinate.Column);
        }
        public bool IsTileValidAndEmpty(int row, int col)
        {
            return !IsTileOccupied(row, col) && IsCoordinateValid(row, col);
        }
        
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
        public void UnsetTile(int row, int col)
        {
            GridTile existingTile = GetTile(row, col);
            Tiles[row, col] = null;
            existingTile.OnDespawn();
            Destroy(existingTile.gameObject);
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
        /// Finds the largest square window with empty tiles on the board
        /// </summary>
        /// <returns></returns>
        public int GetLargestEmptyTileWindow()
        {
            int largestSquare = 1;
            for (int r = 0; r < RowCount; ++r)
            {
                for (int c = 0; c < ColumnCount; ++c)
                {
                    int windowSize = FindEmptyWindowSizeAtRowCol(r, c);
                    largestSquare = Mathf.Max(largestSquare, windowSize);
                }
            }

            return largestSquare;
        }
        
        /// <summary>
        /// Finds the empty window size at given row and col
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        [Button("Find window size")]
        private int FindEmptyWindowSizeAtRowCol(int row, int col)
        {
            int windowSize = 0;
            for (int r = 0; r < RowCount; ++r)
            {
                for (int c = 0; c < ColumnCount; ++c)
                {
                    bool occupied = IsTileOccupied(r + row, c + col);
                    if (occupied) return windowSize;
                    if (c == r && c > 0 && r > 0)
                    {
                        windowSize = r;
                        break; //Continue from row
                    }
                }
            }
            return windowSize;
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

        public List<GridTile> Get4Neighs(int row, int col)
        {
            List<GridTile> neighs = new List<GridTile>();
            for (int i = -1; i < 2; ++i)
            {
                for (int j = -1; j < 2; ++j)
                {
                    if(i==j || (i != 0 && j != 0)) continue; //4 neighs condition
                    GridTile tile = GetTile(row + i, col + j);
                    if(tile == null) continue;
                    neighs.Add(tile);
                }
            }

            return neighs;
        }
        
        /// <summary>
        /// Returns the background object for given row and col
        /// </summary>
        /// <param name="coord"></param>
        /// <returns></returns>
        public GridTileBackground GetBackground(GridTileCoordinate coord)
        {
            if (!IsCoordinateValid(coord)) return null;
            return BackgroundObjects[coord.Row, coord.Column];
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
        /// Returns the corresponding row and col from a world position. The world position is first projected onto the board
        /// </summary>
        /// <param name="position">World position</param>
        /// <returns></returns>
        public GridTileCoordinate GetRowColFromPosition(Vector3 position)
        {
            GridTileCoordinate coord = new GridTileCoordinate();
            Vector3 pointOnBoard = GetPointOnPlane(position);
            return GetRowColFromPointOnBoard(pointOnBoard);
        }
        /// <summary>
        /// Returns row and col 
        /// </summary>
        /// <param name="pointOnGrid"></param>
        public GridTileCoordinate GetRowColFromPointOnBoard(Vector3 pointOnGrid)
        {
            GridTileCoordinate coord = new GridTileCoordinate();
            Vector3 localBotLeft = -ForwardVector * GetDepth() * 0.5f - RightVector * GetWidth() * 0.5f;
            Vector3 botLeftPoint = transform.TransformPoint(localBotLeft);
            Vector3 diff = pointOnGrid - botLeftPoint;
            Vector3 localDiff = transform.InverseTransformDirection(diff);
            float horDist = Kuantech.Utils.Helpers.DotProjection(localDiff, RightVector);
            float depthDist = Kuantech.Utils.Helpers.DotProjection(localDiff, ForwardVector);
            coord.Column = Mathf.FloorToInt(horDist / CellWidth);
            coord.Row = Mathf.FloorToInt(depthDist / CellHeight);
            return coord;
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
        public Vector3 GetBoardNormal()
        {
            return Vector3.Cross(RightVector, ForwardVector);
        }
        
        public Vector3 GetPointOnPlane(Vector3 globalPosition)
        {
            
            Vector3 diff = globalPosition - transform.position;
            Vector3 projectedOntoNormal = Helpers.ProjectVector(diff, GetBoardNormal());
            if (Mathf.Approximately(projectedOntoNormal.sqrMagnitude, 0f))
            {
                return globalPosition;
            }
            Ray ray = new Ray(globalPosition, GetBoardNormal());
            return GetPointOnPlane(ray);
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
        public Vector3 GetLocalPosition(float row, float col, Vector2 originOffset)
        {
            Vector3 horizontalPosition = RightVector * (col * CellWidth + CellWidth * ColumnCount * originOffset.x + CellWidth * 0.5f);
            Vector3 depthPosition = ForwardVector * (row * CellHeight + CellHeight * RowCount * originOffset.y + CellHeight * 0.5f);
            return horizontalPosition + depthPosition;
        }
        
        public Vector3 GetLocalPosition(float row, float col)
        {
            return GetLocalPosition(row, col, OriginOffset);
        }
        
        public Vector3 GetGlobalPosition(float row, float col)
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

        #region Tile Highlighting

        private HashSet<GridTileBackground> _highlightedTiles;
        
        /// <summary>
        /// Clears the highlighted tile backgrounds
        /// </summary>
        public void ClearHighlightedTiles()
        {
            if (_highlightedTiles == null) return;
            foreach (var bg in _highlightedTiles)
            {
                bg.ClearHighlight();
            }
        }
        
        /// <summary>
        /// Highlights the selected tiles
        /// </summary>
        /// <param name="coordinates"></param>
        public void HighlightTiles(List<GridTileCoordinate> coordinates)
        {
            ClearHighlightedTiles();
            if(_highlightedTiles == null) _highlightedTiles = new HashSet<GridTileBackground>();
            foreach (var coord in coordinates)
            {
                GridTileBackground bgObj = GetBackground(coord);
                if (bgObj == null) continue;
                bgObj.Highlight();
                _highlightedTiles.Add(bgObj);
            }
        }
        #endregion
    }
}