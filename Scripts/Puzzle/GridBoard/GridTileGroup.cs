using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Puzzle
{
    [Serializable]
    public struct NeighTileData
    {
        public GridTileCoordinate LocalCoordinates;
        public GridTile NeighTile;
    }
    
    /// <summary>
    /// Represents a group of grid tiles
    /// </summary>
    public class GridTileGroup : MonoBehaviour
    {
        public List<NeighTileData> ChildTilesList;
        public Dictionary<GridTileCoordinate, GridTile> ChildTiles;

        private Vector2 _boundingBoxCenter;
        private Vector2 _boundingBox;

        //Events
        public UnityAction OnPlacedOnBoard;
        public virtual void Initialize()
        {
            ChildTiles = new Dictionary<GridTileCoordinate, GridTile>();
            foreach (var childListElement in ChildTilesList)
            {
                ChildTiles[childListElement.LocalCoordinates] = childListElement.NeighTile;
            }
        }
        
        /// <summary>
        /// Tries to add grid tile to the group
        /// </summary>
        /// <param name="coord">Local grid coordinate</param>
        /// <param name="tile">Tile to add</param>
        /// <returns>True if added, false if not</returns>
        public bool AddTile(GridTileCoordinate coord, GridTile tile)
        {
            ChildTiles ??= new Dictionary<GridTileCoordinate, GridTile>();
            if (ChildTiles.ContainsKey(coord) && ChildTiles[coord] != null) return false;
            ChildTiles[coord] = tile;
            tile.transform.SetParent(transform);
            return true;
        }
        
        /// <summary>
        /// Checks whether the tile group can be placed to given row and col
        /// </summary>
        /// <param name="board"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public bool CanBePlacedToBoard(GridBoard board, int row, int col)
        {
            foreach (var pair in ChildTiles)
            {
                GridTileCoordinate coord = pair.Key;
                if (!board.IsTileValidAndEmpty(coord.Row + row, coord.Column + col)) return false;
            }

            return true;
        }
        
        /// <summary>
        /// Checks if the tile group can be placed anywhere on the board
        /// </summary>
        /// <param name="board"></param>
        /// <returns></returns>
        public bool CanBePlacedToBoard(GridBoard board)
        {
            for (int r = 0; r < board.RowCount; ++r)
            {
                for (int c = 0; c < board.ColumnCount; ++c)
                {
                    if (CanBePlacedToBoard(board, r, c)) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Places the group on the board
        /// </summary>
        /// <param name="board"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        public virtual bool PlaceOnBoard(GridBoard board, int row, int col)
        {
            foreach (var pair in ChildTiles)
            {
                GridTileCoordinate localCoord = pair.Key;
                GridTile tile = pair.Value;
                if (!board.MoveTile(tile, localCoord.Row + row, localCoord.Column + col)) return false;
            }
            OnPlacedOnBoard?.Invoke();
            return false;
        }

        public void SetTilePositions(GridBoard board)
        {
            Vector3 boundingBoxCenter = GetBoundingBoxCenter(board);
            foreach (var pair in ChildTiles)
            {
                GridTileCoordinate coord = pair.Key;
                GridTile childTile = pair.Value;
                Vector3 localCoord = GetLocalPosition(board, coord.Row, coord.Column);
                childTile.transform.localPosition = localCoord - boundingBoxCenter;
            }
        }

        public void SetTilePosition(GridTile tile, GridTileCoordinate coordinate, GridBoard parentBoard)
        {
            Vector3 localCoord = GetLocalPosition(parentBoard, coordinate.Row, coordinate.Column);
            tile.transform.localPosition = localCoord;
        }
        
        public Vector3 GetBoundingBoxCenter(GridBoard board)
        {
            Vector2Int rowLimits = new Vector2Int(Int32.MaxValue, Int32.MinValue);
            Vector2Int colLimits = new Vector2Int(Int32.MaxValue, Int32.MinValue);
            foreach (var pair in ChildTiles)
            {
                //Min and max row
                rowLimits.x = Mathf.Min(rowLimits.x, pair.Key.Row);
                rowLimits.y = Mathf.Max(rowLimits.y, pair.Key.Row);
                
                //Min and max col
                colLimits.x = Mathf.Min(colLimits.x, pair.Key.Column);
                colLimits.y = Mathf.Max(colLimits.y, pair.Key.Column);
            }

            float midRow = (rowLimits.y + rowLimits.x) * 0.5f;
            float midCol = (colLimits.y + colLimits.x) * 0.5f;
            return GetLocalPosition(board, midRow, midCol);
        }

        public Vector2Int GetBoundingBoxSize()
        {
            Vector2Int rowLimits = new Vector2Int(Int32.MaxValue, Int32.MinValue);
            Vector2Int colLimits = new Vector2Int(Int32.MaxValue, Int32.MinValue);
            foreach (var pair in ChildTiles)
            {
                //Min and max row
                rowLimits.x = Mathf.Min(rowLimits.x, pair.Key.Row);
                rowLimits.y = Mathf.Max(rowLimits.y, pair.Key.Row);
                
                //Min and max col
                colLimits.x = Mathf.Min(colLimits.x, pair.Key.Column);
                colLimits.y = Mathf.Max(colLimits.y, pair.Key.Column);
            }

            return new Vector2Int(colLimits.y - colLimits.x + 1, rowLimits.y-rowLimits.x + 1);
        }
        
        public Vector3 GetAnchorTilePosition(GridBoard board)
        {
            var firstKey = ChildTiles.Keys.ToArray()[0];
            GridTile tile = ChildTiles[firstKey];

            Vector3 globalPosition = tile.transform.position;
            
            //Get the offset by using local row and col
            Vector3 localPosition = GetLocalPosition(board, firstKey.Row, firstKey.Column);

            return globalPosition - localPosition;
        }
        
        /// <summary>
        /// Gets the local position for a tile in GridTileGroup. Not quite the same as with the function in GridBoard
        /// </summary>
        /// <returns></returns>
        public Vector3 GetLocalPosition(GridBoard board, float row, float col)
        {
            return board.RightVector * (col * board.CellWidth) + board.ForwardVector * (row * board.CellHeight);
        }
    }
}