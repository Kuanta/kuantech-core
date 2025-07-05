using UnityEngine;

namespace Kuantech.Puzzle
{
    public abstract class BoardTile : MonoBehaviour
    {
        public Board ParentBoard;
        public BoardTileCoordinate CurrentCoordinate;
        
        public bool IsExisting;
        public bool DestroyOnDespawn = true;
        public bool StayOnBoardAfterDespawn = false;
        
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
        
        #region Events

        public virtual void OnSetToBoard()
        {
            
        }
        
        public virtual void OnUnsetFromBoard()
        {
            
        }
        #endregion
    }
}