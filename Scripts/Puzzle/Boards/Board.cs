using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Puzzle
{
    public abstract class BoardTileCoordinate
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

        public abstract BoardTileCoordinate GetGlobalCoordinate(BoardTileCoordinate localCoordinate);

    }
    
    public abstract class Board : MonoBehaviour
    {
        [Header("Existing Tiles")] 
        public List<GameObject> ExistingLayers;
        
        //Events
        public UnityAction<BoardTile> OnTilePlacedToBoardEvent;
        
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
            if (!IsCoordinateValid(coordinate) || !tile.CanBePlacedToBoard(this)) return false;
            //Single tile
            if (tile.Coordinates.IsNullOrEmpty())
            {
                BoardTile existingTile = GetTile(coordinate);
                if (existingTile == null) return true;
                if (existingTile != tile && existingTile.CanBeMergedWith(tile))
                {
                    return true;
                }
                return false;

            }
            
            //Check multi grid 
            bool allEmpty = true;
            List<BoardTile> tilesThatCanBeMerged = new List<BoardTile>();
            foreach (var local in tile.Coordinates)
            {
                if (!IsCoordinateValid(local)) return false; // All coords must be valid
                BoardTileCoordinate globalCoord = coordinate.GetGlobalCoordinate(local);
                BoardTile existingTile = GetTile(globalCoord);
                if (existingTile != null)
                {
                    if(existingTile == tile) continue;
                    allEmpty = false;
                    if (existingTile.CanBeMergedWith(tile))
                    {
                        tilesThatCanBeMerged.Add(tile);
                    }
                }
            }
            
            if (allEmpty) return true; //All empty
            if (tilesThatCanBeMerged.IsNullOrEmpty()) return false; //Not all empty but there is no tile that can be merged
            return true;
        }
        
        /// <summary>
        /// Sets the tile to board
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="coordinate"></param>
        /// <param name="setPosition"></param>
        /// <returns></returns>
        public virtual bool SetTile(BoardTile tile, BoardTileCoordinate coordinate, bool setPosition = true)
        {
            if (!CanSetTile(tile, coordinate)) return false;
            if (tile.ParentBoard != null)
            {
                //Clear previous position
                ClearTileArrayForTile(tile);
            }
            List<BoardTile> tilesThatCanBeMerged = new List<BoardTile>();
            if (!tile.Coordinates.IsNullOrEmpty())
            {
                //Check merge
                foreach (var coord in tile.Coordinates)
                {
                    BoardTileCoordinate global = coordinate.GetGlobalCoordinate(coord);
                    BoardTile tileAtCoord = GetTile(global);
                    if (tileAtCoord == null) continue;
                    if (tileAtCoord.CanBeMergedWith(tile))
                    {
                        tilesThatCanBeMerged.Add(tileAtCoord);
                    }
                }
            }
            else
            {
                BoardTile existingTile = GetTile(coordinate);
                if (existingTile != null && existingTile.CanBeMergedWith(tile))
                {
                    tilesThatCanBeMerged.Add(existingTile);
                }
            }

            if (tilesThatCanBeMerged.Count > 0)
            {
                //Merge with the first one
                tilesThatCanBeMerged[0].MergeWith(tile);
                OnTileMerged(tilesThatCanBeMerged[0]);
                return true;
            }
            
            //No merge, place on the board
            tile.ParentBoard = this;
            SetTileArrayForTile(tile, coordinate);
            RegisterTileToCoordinate(tile, coordinate);
            tile.OnSetToBoard();
            if (setPosition)
            {
                PositionTileAtCoordinate(tile, coordinate);
            }
            OnTileSetToBoard(tile);
            return true;
        }

        protected virtual void OnTileMerged(BoardTile tile)
        {
            
        }
        
        protected virtual void OnTileSetToBoard(BoardTile tile)
        {
            OnTilePlacedToBoardEvent?.Invoke(tile);
        }
        
        public virtual void PositionTileAtCoordinate(BoardTile tile, BoardTileCoordinate coordinate)
        {
            throw new System.NotImplementedException();
        }
        
        /// <summary>
        /// Removes the tile from the board
        /// </summary>
        /// <param name="tile"></param>
        public virtual void UnsetTile(BoardTile tile)
        {
            ClearTileArrayForTile(tile);
            tile.ParentBoard = null;
            tile.CurrentCoordinate = null;
            
        }
        
        public abstract BoardTile GetTile(BoardTileCoordinate qbertTileCoordinate);
        
        public abstract List<T> GetTiles<T>() where T : BoardTile;
        
        /// <summary>
        /// Saves the tile. Every board must have its own implementation
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="coordinate"></param>
        protected abstract void RegisterTileToCoordinate(BoardTile tile, BoardTileCoordinate coordinate);
        
        /// <summary>
        /// Clears the tile info
        /// </summary>
        /// <param name="tile"></param>
        protected abstract void UnsetCoordinate(BoardTileCoordinate coordinate);
        
        /// <summary>
        /// Simply fills the tile array.
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="anchorRow"></param>
        /// <param name="anchorColumn"></param>
        /// <param name="anchorLayer"></param>
        public virtual void SetTileArrayForTile(BoardTile tile, BoardTileCoordinate anchorCoord)
        {
            tile.CurrentCoordinate = anchorCoord;
            if (tile.Coordinates.IsNullOrEmpty())
            {
                RegisterTileToCoordinate(tile, anchorCoord);
                return;
            }
            foreach (var localCoordinate in tile.Coordinates)
            {
                BoardTileCoordinate boardTileCoordinate = anchorCoord.GetGlobalCoordinate(localCoordinate);
                RegisterTileToCoordinate(tile, boardTileCoordinate);
            }
        }
        
        /// <summary>
        /// Clears tile array
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="coordinate"></param>
        public virtual void ClearTileArrayForTile(BoardTile tile)
        {
            if (tile.Coordinates.IsNullOrEmpty())
            {
                UnsetCoordinate(tile.CurrentCoordinate);
                return;
            }
            foreach (var localCoord in tile.Coordinates)
            {
                BoardTileCoordinate boardTileCoordinate = tile.CurrentCoordinate.GetGlobalCoordinate(localCoord);
                UnsetCoordinate(boardTileCoordinate);
            }
        }
        #endregion

        #region Tile Coordinates
        public abstract BoardTileCoordinate GetTileCoordinateFromWorldPosition(Vector3 worldPosition);
        
        public abstract Vector3 GetLocalPosition(BoardTileCoordinate tileCoordinate);
        
        public abstract bool IsCoordinateValid(BoardTileCoordinate tileCoordinate);
        
        #endregion
    }
}