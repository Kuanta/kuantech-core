using System;
using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Puzzle
{
    public abstract class BoardTile : MonoBehaviour
    {
        [Header("Tile Properties")]
        [SerializeField] private string TileId;
        [Tooltip("A filter for what layers this tile can be placed on (if empty, can be placed on any layer)")]
        [SerializeField] private List<int> AllowedLayers = null;
        
        [Tooltip("If this tile is an unmasker, what layers it will unmask (if empty, will unmask all layers)")]
        [SerializeField] private List<int> LayersToUnmask = null;
        
        [Header("Despawn Settings")]        
        public bool DestroyOnDespawn = true;
        public bool StayOnBoardAfterDespawn = false;
        [Tooltip("Not a common behaviour but could be useful for unmaskers")]
        public bool DespawnOnPlaced = false;
        
        //Runtime
        [NonSerialized] public bool IsExisting;
        [NonSerialized] public Board ParentBoard;
        [NonSerialized] public BoardTileCoordinate CurrentCoordinate; //Anchor coordinate
        
        public virtual string GetTileId()
        {
            if (TileId.IsNullOrEmpty())
            {
                throw new Exception("TileId is null or empty");
                return null;
            }

            return TileId;
        }
        
        //Multi coord Board Tile
        [SerializeReference]
        public List<BoardTileCoordinate> Coordinates;
        
        public abstract List<BoardTileCoordinate> GetOccupiedCoordinates();
        
        public virtual void Spawn(bool isExisting = false)
        {
            IsExisting = isExisting;
        }
        
        public virtual void Despawn(bool clearingBoard)
        {
            if (!clearingBoard && StayOnBoardAfterDespawn)
            {
                gameObject.SetActive(false);
                return;
            }
            if (ParentBoard != null)
            {
                ParentBoard.UnsetTile(this);
            }
            if (DestroyOnDespawn)
            {
                Destroy(gameObject);
            }
            else if(!StayOnBoardAfterDespawn)
            {
                gameObject.SetActive(false);
            }
        }

        public virtual bool IsPlacedToBoard()
        {
            return ParentBoard != null;
        }

        public virtual bool CanBePlacedToBoard(Board board)
        {
            return true;
        }

        public virtual bool CanBeMergedWith(BoardTile other)
        {
            if (other == null || other == this) return false;
            return true;
        }
        
        /// <summary>
        /// Merges this tile with other. Other will be despawned
        /// </summary>
        /// <param name="other"></param>
        public virtual bool MergeWith(BoardTile other)
        {
            if (other == null || !CanBeMergedWith(other)) return false;
            other.OnMergedWith(this);
            return true;
        }
        
        #region Events

        public virtual void OnSetToBoard()
        {
            UnmaskLayers();
            if(DespawnOnPlaced)
            {
                Despawn(false);
            }
        }
        
        public virtual void OnUnsetFromBoard()
        {
            //todo: Discuss if unmasked layers should be masked back
        }
        
        /// <summary>
        /// Called for the dropped tile
        /// </summary>
        /// <param name="mergedWith"></param>
        public virtual void OnMergedWith(BoardTile mergedWith)
        {
           Despawn(false);
        }
        #endregion
        
        #region Unmasker
        //Unmasks the layer
        public void UnmaskLayers()
        {
            if (ParentBoard == null || LayersToUnmask.IsNullOrEmpty()) return;
            foreach (var layer in LayersToUnmask)
            {
                List<BoardTileCoordinate> coords = GetOccupiedCoordinates();
                foreach(var coord in coords)
                {
                    coord.Layer = layer;
                    ParentBoard.ClearMask(coord);
                }
            }
            ParentBoard.UpdateBackgroundTileVisibilities();
        }
        #endregion
        
        #region State
        public virtual Board.BoardTileState GetBoardTileState()
        {
            Board.BoardTileState newBoardTileState = new Board.BoardTileState
            {
                AnchorCoordinates = CurrentCoordinate,
                LocalCoordinates = Coordinates,
                TileTypeId = GetTileId(),
                CustomData = GetCustomData(),
            };

            return newBoardTileState;
        }

        public virtual void LoadBoardTileState(Board.BoardTileState state)
        {
            LoadCustomData(state.CustomData);
        }

        /// <summary>
        /// If a tile has custom data, this is the place to provide that custom data
        /// </summary>
        /// <returns></returns>
        public virtual byte[] GetCustomData()
        {
            return null;
        }

        public virtual void LoadCustomData(byte[] customData)
        {
            
        }
        #endregion
    }
}