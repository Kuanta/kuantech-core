using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Puzzle
{
    public abstract class BoardTile : MonoBehaviour
    {
        public Board ParentBoard;
        public BoardTileCoordinate CurrentCoordinate; //Anchor coordinate
        
        public bool IsExisting;
        public bool DestroyOnDespawn = true;
        public bool StayOnBoardAfterDespawn = false;
        
        //Multi coord Board Tile
        public List<BoardTileCoordinate> Coordinates;
        
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
            
        }
        
        public virtual void OnUnsetFromBoard()
        {
            
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
    }
}