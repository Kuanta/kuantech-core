using System;
using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Puzzle
{
    [Serializable]
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
        
        
        public override bool Equals(object obj)
        {
            if (obj is not BoardTileCoordinate other) return false;
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
    
    public abstract class Board : MonoBehaviour
    {
        [Header("Board Properties")]
        public List<int> MaskedLayersAtStartup = new List<int>();
        
        [Header("Tile Collection")] 
        public BoardTileCollection BoardTileCollection;
        
        [Header("Existing Tiles")] 
        public List<GameObject> ExistingLayers;
        
        [Header("Background Tiles")]
        public BoardTileBackground BackgroundGameObjectPrefab;
        [Tooltip("When creating background tiles, if the coordinate in these layers is masked, no background tile will be created")]
        public List<int> BackgroundMaskingLayers = null;
        protected Dictionary<BoardTileCoordinate, BoardTileBackground> BackgroundTiles = new Dictionary<BoardTileCoordinate, BoardTileBackground>(); 
        public HashSet<BoardTileCoordinate> BackgroundMask;
        
        //Board masking
        public HashSet<BoardTileCoordinate> MaskedCoordinates = new HashSet<BoardTileCoordinate>();
        
        //Events
        public UnityAction<BoardTile, bool> OnTilePlacedToBoardEvent;
        public UnityAction<BoardTile> OnTileMergedEvent;
        
        //Editor
        [Header("Editor Background")] 
        [SerializeField] protected GameObject Editorbackground;

        public virtual void CreateBoard()
        {
            BackgroundTiles = new Dictionary<BoardTileCoordinate, BoardTileBackground>();
            BackgroundMask = new HashSet<BoardTileCoordinate>();
            SetBackgroundTiles();
            if(MaskedLayersAtStartup.IsNullOrEmpty()) return;
            foreach(var layerIndex in MaskedLayersAtStartup)
            {
                MaskAllCoordinates(layerIndex);
            }
            UpdateBackgroundTileVisibilities();
        }
        
        /// <summary>
        /// Returns all possible coordinates on the board
        ///</summary>
        public virtual List<BoardTileCoordinate> GetAllCoordinates(int layer=0)
        {
            throw new NotImplementedException("GetAllCoordinates not implemented for this board type");
        }
        
        #region Background Tiles
        /// <summary>
        /// Creates the background tiles
        /// </summary>
        protected void SetBackgroundTiles()
        {
            //Clear existing ones first
            if(!BackgroundTiles.IsNullOrEmpty())
            {
                foreach(var background in BackgroundTiles.Values)
                {
                    if(background != null) Helpers.DestroyGameObject(background.gameObject);
                }
            }
            
            BackgroundTiles = new Dictionary<BoardTileCoordinate, BoardTileBackground>();
            List<BoardTileCoordinate> coordinates = GetAllCoordinates();
            foreach(var coord in coordinates)
            {
                AddBackgroundObject(coord);
            }
        }
           
        public virtual void UpdateBackgroundTileVisibilities()
        {
            foreach(var pair in BackgroundTiles)
            {
                BoardTileCoordinate coord = pair.Key;
                BoardTileBackground bgObj = pair.Value;
                if(bgObj == null) continue;
                bool isMasked = IsBackgroundMasked(coord);
                bgObj.SetMasked(isMasked);
            }
        }
        
        public void MaskBackground(BoardTileCoordinate coordinate)
        {
            if (!BackgroundMask.Contains(coordinate))
            {
                BackgroundMask.Add(coordinate);
            }
        }
        
        public void ClearBackgroundMask(BoardTileCoordinate coordinate)
        {
            if (BackgroundMask.Contains(coordinate))
            {
                BackgroundMask.Remove(coordinate);
            }
        }
        
        public virtual bool IsBackgroundMasked(BoardTileCoordinate coordinate)
        {
            return BackgroundMask.Contains(coordinate) || IsTileMasked(coordinate);
        }
        
        protected virtual BoardTileBackground AddBackgroundObject(BoardTileCoordinate coordinate)
        {
            if (BackgroundGameObjectPrefab != null)
            {
                BoardTileBackground bgObj = Instantiate(BackgroundGameObjectPrefab);
                bgObj.transform.parent = transform;
                BackgroundTiles[coordinate] = bgObj;
                return bgObj;
            }
            return null;
        }
        
        ///Toggles the background
        public void ToggleBackgroundObject(BoardTileCoordinate coordinate, bool toggle)
        {   
            BoardTileBackground bgObj = GetBackground(coordinate);
            if(bgObj == null) return;
            bgObj.gameObject.SetActive(toggle);
        }
        
        public BoardTileBackground GetBackground(BoardTileCoordinate coord)
        {
            if (!IsCoordinateValid(coord)) return null;
            return BackgroundTiles[coord];
        }
        #endregion
        
        #region Tile Operations
        public virtual bool IsTileValidAndEmpty(BoardTileCoordinate tileCoordinate)
        {
            return true;
        }
        
        public virtual bool CanSetTile(BoardTile tile, BoardTileCoordinate coordinate)
        {
            if (!IsCoordinateValid(coordinate) || IsTileMasked(coordinate) || !tile.CanBePlacedToBoard(this)) return false;
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
        public virtual bool SetTile(BoardTile tile, BoardTileCoordinate coordinate, bool setPosition = true, bool fromLoadState=false)
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
            OnTileSetToBoard(tile, fromLoadState);
            return true;
        }

        protected virtual void OnTileMerged(BoardTile tile)
        {
            OnTileMergedEvent?.Invoke(tile);
        }
        
        protected virtual void OnTileSetToBoard(BoardTile tile, bool fromLoadState)
        {
            OnTilePlacedToBoardEvent?.Invoke(tile, fromLoadState);
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

        public abstract List<BoardTile> GetAllTiles();
        
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
        
        /// <summary>
        /// Clears the board
        /// </summary>
        public virtual void ClearBoard()
        {
            List<BoardTile> boards = GetAllTiles();
            foreach (var boardTile in boards)
            {
                UnsetTile(boardTile);
                boardTile.Despawn(true);
            }
        }
        
        #endregion

        #region Tile Coordinates
        public abstract BoardTileCoordinate GetTileCoordinateFromWorldPosition(Vector3 worldPosition);
        
        public abstract Vector3 GetLocalPosition(BoardTileCoordinate tileCoordinate);
        
        public abstract bool IsCoordinateValid(BoardTileCoordinate tileCoordinate);
       
        #endregion
        
        #region Board Masking
        
        /// <summary>
        /// Returns unmasked tile coordinates for a layer
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        public List<BoardTileCoordinate> GetUnmaskedTileCoordinates(int layer)
        {
            List<BoardTileCoordinate> allCoords = GetAllCoordinates(layer);
            List<BoardTileCoordinate> unmaskedCoords = new List<BoardTileCoordinate>();
            foreach (var coord in allCoords)
            {
                coord.Layer = layer;
                if (!IsTileMasked(coord))
                {
                    unmaskedCoords.Add(coord);
                }
            }

            return unmaskedCoords;
        }
        public virtual void MaskAllCoordinates(int layer)
        {
            throw new NotImplementedException("MaskAllCoordinates not implemented for this board type");
        }
        
        public virtual void UnmaskAllCoordinates(int layer)
        {
            throw new NotImplementedException("UnMaskAllCoordinates not implemented for this board type");
        }
        
        public bool IsTileMasked(BoardTileCoordinate coordinate)
        {
            return MaskedCoordinates.Contains(coordinate);
        }
        
        public void MaskCoordinate(BoardTileCoordinate coordinate)
        {
            if (!MaskedCoordinates.Contains(coordinate))
            {
                MaskedCoordinates.Add(coordinate);
            }
        }
        
        public void ClearMask(BoardTileCoordinate coordinate)
        {
            if (MaskedCoordinates.Contains(coordinate))
            {
                MaskedCoordinates.Remove(coordinate);
            }
        }
        #endregion 
        
        #region State
        
        [Serializable]
        public class BoardState
        {
            public List<BoardTileState> TileStates;
        }
        
        [Serializable]
        public class BoardTileState
        {
            public BoardTileCoordinate AnchorCoordinates;
            public List<BoardTileCoordinate> LocalCoordinates;
            public string TileTypeId;
            public byte[] CustomData;
        }

        public BoardState GetBoardState()
        {
            BoardState boardState = new BoardState();
            boardState.TileStates = new List<BoardTileState>();
            List<BoardTile> allPlacedTiles = GetAllTiles();
            foreach (var tile in allPlacedTiles)
            {
                boardState.TileStates.Add(tile.GetBoardTileState());
            }
            return boardState;
        }

        public void LoadBoardState(BoardState boardState)
        {
            ClearBoard();
            if (BoardTileCollection == null)
            {
                Debug.LogWarning("No collection is set, cant load the board");
                return;
            }
            
            //Instantiate and place tiles
            foreach(var boardTileState in boardState.TileStates)
            {
                BoardTile tilePrefab = BoardTileCollection.GetTilePrefab(boardTileState.TileTypeId);
                if (tilePrefab == null)
                {
                    Debug.LogError($"Tile prefab with id {boardTileState.TileTypeId} not found in collection");
                    continue;
                }
                BoardTile newTile = Instantiate(tilePrefab);
                SetTile(newTile, boardTileState.AnchorCoordinates, true, true);
                newTile.LoadBoardTileState(boardTileState);
            }
        }
        #endregion
    }
}