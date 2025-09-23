using System;
using System.Collections.Generic;
using Kuantech.AI.Pathfinding;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Puzzle
{   

    [Serializable]
    public struct GridTileData
    {
        public string Id;
        public GridTile Prefab;
    }
    
    [Serializable]
    public class GridTileCoordinate : BoardTileCoordinate
    {
        public GridTileCoordinate()
        {
            Row = 0;
            Column = 0;
            Layer = 0;
        }
        public GridTileCoordinate(int row, int col, int layer = 0)
        {
            Row = row;
            Column = col;
            Layer = layer;
        }
        
        public static GridTileCoordinate FromVector2(Vector2Int rowCol)
        {
            return new GridTileCoordinate()
            {
                Row = rowCol.y,
                Column = rowCol.x,
            };
        }
        public static GridTileCoordinate operator +(GridTileCoordinate coord, Vector2Int offset)
        {
            return new GridTileCoordinate
            (
                row: coord.Row + offset.y,
                col: coord.Column + offset.x,
                layer: coord.Layer
            );
        }

        public static GridTileCoordinate operator +(GridTileCoordinate coord, GridTileCoordinate other)
        {
            return new GridTileCoordinate
            (
                row: coord.Row + other.Row,
                col: coord.Column + other.Column,
                layer: coord.Layer
            );
        }

        public override BoardTileCoordinate GetGlobalCoordinate(BoardTileCoordinate localCoordinate)
        {
            return new GridTileCoordinate()
            {
                Row = Row + localCoordinate.Row,
                Column = Column + localCoordinate.Column,
                Layer = Layer + localCoordinate.Layer,
            };
        }
        public override bool Equals(object obj)
        {
            if (obj is not GridTileCoordinate other) return false;
            return Row == other.Row && Column == other.Column && Layer == other.Layer && Height == other.Height;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Row.GetHashCode();
                hash = hash * 23 + Column.GetHashCode();
                hash = hash * 23 + Layer.GetHashCode();
                return hash;
            }
        }
    }
    
    public class GridTile : BoardTile
    {
        [Tooltip("Unique id for the tile type")]
        public int GridTypeId;
        public Vector3 AnchorOffset;

        public int AnchorRow => GetAnchorRow();
        public int AnchorColumn => GetAnchorColumn();
        public int AnchorLayer => GetAnchorLayer();
        
        
        [NonSerialized] public Vector3Int MinCoords;
        [NonSerialized] public Vector3Int MaxCoords;
        [NonSerialized] public bool IsExisting;

        [Header("Visual")]
        public bool MaskBackground = false;
        public Transform VisualParent;
        [NonSerialized] public GameObject CurrentVisual;
        public bool LockVisual = false;

        public virtual void InitializeExisting()
        {
            
        }

        public bool IsNeighbourWithTile(GridTile tile)
        {
            //Check if this is neighbour with other tile
            int rowDiff = Mathf.Abs(tile.GetAnchorRow() - GetAnchorRow());
            int colDiff = Mathf.Abs(tile.GetAnchorColumn() - GetAnchorColumn());
            int layerDiff = Mathf.Abs(tile.GetAnchorLayer() - GetAnchorLayer());
            return (rowDiff + colDiff + layerDiff <= 1);
        }


        #region Coorddinates

        public int GetAnchorRow()
        {
            if (CurrentCoordinate == null || !(CurrentCoordinate is GridTileCoordinate gridTileCoordinate)) return -1;
            return gridTileCoordinate.Row;
        }

        public int GetAnchorColumn()
        {
            if (CurrentCoordinate == null || !(CurrentCoordinate is GridTileCoordinate gridTileCoordinate)) return -1;
            return gridTileCoordinate.Column;
        }

        public int GetAnchorLayer()
        {
            if (CurrentCoordinate == null || !(CurrentCoordinate is GridTileCoordinate gridTileCoordinate)) return -1;
            return gridTileCoordinate.Layer;
        }
        
        /// <summary>
        /// Returns the anchor coordinates
        /// </summary>
        /// <returns></returns>
        public GridTileCoordinate GetCurrentCoordinate()
        {
            return CurrentCoordinate as GridTileCoordinate;
        }
        public virtual void SetRowCol(int row, int col, int layer)
        {
            CurrentCoordinate = new GridTileCoordinate()
            {
                Row = row,
                Column = col,
                Layer = layer,
            };
        }
        #endregion
        
        
        public void SetLocalPosition(Vector3 localPosition)
        {
            transform.localPosition = localPosition;
        }

        public virtual void SetVisual(GameObject visual)
        {
            if(CurrentVisual != null && LockVisual) return;
            if(CurrentVisual != null)
            {
                Destroy(CurrentVisual);
            }
            CurrentVisual = Instantiate(visual);
            Transform visualParent = VisualParent != null ? VisualParent : transform;
            CurrentVisual.transform.SetParent(visualParent);
            CurrentVisual.transform.localPosition = Vector3.zero;
            CurrentVisual.transform.localRotation = Quaternion.identity;
        }
        
        /// <summary>
        /// Returns the path node on currently standing tile
        /// </summary>
        /// <returns></returns>
        public virtual PathNode GetCurrentPathNode()
        {
            if (ParentBoard == null) return null;
            return (ParentBoard as GridBoard)?.GetPathNodeAtCoordinate(new GridTileCoordinate()
            {
                Row = GetAnchorRow(),
                Column = GetAnchorColumn(),
                Layer = GetAnchorLayer(),
            });
        }

        public void RemoveFromBoard()
        {
            if (ParentBoard == null) return;
            ParentBoard.UnsetTile(this);
        }

        public virtual Vector3 GetTileLocalOffset()
        {
            return AnchorOffset;
        }

        public Vector3 GetTileAnchorPosition()
        {
            return transform.position + GetTileLocalOffset();
        }
        public virtual void Reset()
        {
            
        }

        public Vector3 GetBoundingBoxCenter(GridBoard board)
        {
            Vector2Int rowLimits = new Vector2Int(Int32.MaxValue, Int32.MinValue);
            Vector2Int colLimits = new Vector2Int(Int32.MaxValue, Int32.MinValue);
            foreach (var coord in Coordinates)
            {
                //Min and max row
                rowLimits.x = Mathf.Min(rowLimits.x, coord.Row);
                rowLimits.y = Mathf.Max(rowLimits.y, coord.Row);
                
                //Min and max col
                colLimits.x = Mathf.Min(colLimits.x, coord.Column);
                colLimits.y = Mathf.Max(colLimits.y, coord.Column);
            }

            float midRow = (rowLimits.y + rowLimits.x) * 0.5f;
            float midCol = (colLimits.y + colLimits.x) * 0.5f;
            return GetLocalPosition(board, midRow, midCol);
        }
        
        private Vector3 GetLocalPosition(GridBoard board, float row, float col)
        {
            return board.RightVector * (col * board.CellWidth) + board.ForwardVector * (row * board.CellHeight);
        }

        public GridBoard GetParentGridBoard()
        {
            return ParentBoard as GridBoard;
        }

        public BoardTile[] Get4Neighs()
        {
            if (ParentBoard == null) return null;
            return (ParentBoard as GridBoard)?.Get4Neighs(AnchorRow, AnchorColumn, 0);
        }

        public override List<BoardTileCoordinate> GetOccupiedCoordinates()
        {
            if (Coordinates.IsNullOrEmpty()) return new List<BoardTileCoordinate>()
            {
                new GridTileCoordinate()
                {
                    Row = GetAnchorRow(),
                    Column = GetAnchorColumn(),
                    Layer = GetAnchorLayer(),
                }
            };
            List<BoardTileCoordinate> globalCoords = new List<BoardTileCoordinate>();
            foreach (var coord in Coordinates)
            {
                globalCoords.Add(new GridTileCoordinate()
                {
                    Row = GetAnchorRow() + coord.Row,
                    Column = GetAnchorColumn() + coord.Column,
                    Layer = GetAnchorLayer() + coord.Layer,
                });
            }
            return globalCoords;
        }
    }
}