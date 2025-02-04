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
        public int DummyVar;
    }
    
    public class GridTile : BoardTile
    {
        [Tooltip("Unique id for the tile type")]
        public int GridTypeId;
        public bool DestroyOnDespawn = true;
        public bool StayOnBoardAfterDespawn = false;
        public List<GridTileCoordinate> Coordinates;
        [NonSerialized] public GridBoard ParentBoard;
        [NonSerialized] public int AnchorRow;
        [NonSerialized] public int AnchorColumn;
        [NonSerialized] public int AnchorLayer;
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
            int rowDiff = Mathf.Abs(tile.AnchorRow - AnchorRow);
            int colDiff = Mathf.Abs(tile.AnchorColumn - AnchorColumn);
            int layerDiff = Mathf.Abs(tile.AnchorLayer - AnchorLayer);
            return (rowDiff + colDiff + layerDiff <= 1);
        }
        /// <summary>
        /// How many rows does this tile requires
        /// </summary>
        /// <returns></returns>
        public void GetRowSize()
        {
            if (Coordinates.IsNullOrEmpty())
            {
                MaxCoords = Vector3Int.zero;
                MinCoords = Vector3Int.zero;
            }
            
            //todo: Continue this
            Vector3Int maxSizes = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
            Vector3Int minSize = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
            foreach (var coord in Coordinates)
            {
                // int row = coord.Row + AnchorRow;
                // int col = coord.Column + AnchorColumn;
                // int layer = coord.Layer + AnchorLayer;
                // if (row > maxSizes.y)
                // {
                //     maxSizes.y = row;
                // }
            }
        }
        public GridTileCoordinate GetCurrentCoordinates()
        {
            return new GridTileCoordinate()
            {
                Row = AnchorRow,
                Column = AnchorColumn,
                Layer = AnchorLayer,
            };
        }
        public virtual void SetRowCol(int row, int col, int layer)
        {
            AnchorRow = row;
            AnchorColumn = col;
            AnchorLayer = layer;
        }
        
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
            return ParentBoard.GetPathNodeAtCoordinate(new GridTileCoordinate()
            {
                Row = AnchorRow,
                Column = AnchorColumn,
                Layer = AnchorLayer,
            });
        }

        public void RemoveFromBoard()
        {
            if (ParentBoard == null) return;
            ParentBoard.UnsetTile(this);
        }

        public virtual void Reset()
        {
            
        }
    }
}