using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Puzzle
{
    public class BoardTileCoordinate
    {
        public BoardTileCoordinate()
        {
            Layer = 0;
            Row = 0;
            Column = 0;
            Height = 0;
        }
        //Common params
        public int Layer;
        public int Row;
        public int Column;
        
        //Voxel board params
        public int Height;
    }
    
    public abstract class Board : MonoBehaviour
    {
        [Header("Existing Tiles")] 
        public List<GameObject> ExistingLayers;
        
        //Events
        public UnityAction<BoardTile> OnTilePlacedToBoard;
        
        //Editor
        [Header("Editor Background")] 
        [SerializeField] protected GameObject Editorbackground;

        public virtual void CreateBoard()
        {
        }
        
        #region Tile Operations
        public virtual bool IsTileValidAndEmpty(BoardTileCoordinate tileCoordinate)
        {
            return true;
        }
        
        public virtual bool CanSetTile(BoardTile tile, BoardTileCoordinate coordinate)
        {
            if (!IsTileValidAndEmpty(coordinate) || !tile.CanBePlacedToBoard(this)) return false;
            return true;
        }
        
        public virtual bool SetTile(BoardTile tile, BoardTileCoordinate coordinate, bool setPosition = true)
        {
            if (!CanSetTile(tile, coordinate)) return false;
            if (tile.ParentBoard != null)
            {
                //Clear previous position
                tile.ParentBoard.UnsetTile(tile);
            }
            tile.ParentBoard = this;
            tile.CurrentCoordinate = coordinate;
            RegisterTile(tile, coordinate);
            tile.OnSetToBoard();
            OnTilePlacedToBoard?.Invoke(tile);
            return true;
        }

        public virtual void UnsetTile(BoardTile tile)
        {
            UnregisterTile(tile);
            tile.ParentBoard = null;
            tile.CurrentCoordinate = null;
        }
        
        public abstract BoardTile GetTile(BoardTileCoordinate qbertTileCoordinate);
        
        /// <summary>
        /// Saves the tile. Every board must have its own implementation
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="coordinate"></param>
        protected abstract void RegisterTile(BoardTile tile, BoardTileCoordinate coordinate);
        
        /// <summary>
        /// Clears the tile info
        /// </summary>
        /// <param name="tile"></param>
        protected abstract void UnregisterTile(BoardTile tile);
        
        #endregion

        #region Tile Coordinates
        public abstract BoardTileCoordinate GetTileCoordinateFromWorldPosition(Vector3 worldPosition);
        
        public abstract Vector3 GetLocalPosition(BoardTileCoordinate tileCoordinate);
        
        public abstract bool IsCoordinateValid(BoardTileCoordinate tileCoordinate);
        
        #endregion
    }
}