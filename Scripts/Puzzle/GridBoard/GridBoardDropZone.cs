using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Puzzle
{
    [RequireComponent(typeof(GridBoard))]
    public class GridBoardDropZone : MonoBehaviour, IDropZone
    {
        public GridBoard GridBoard;
        public delegate bool HandleDroppedTileHandler(GridTileDraggable draggableTile, int row, int col);
        public HandleDroppedTileHandler DroppedTileHandler;
        
        public delegate bool CanTileDroppedHandler(GridTileDraggable draggableTile, int row, int col);
        public CanTileDroppedHandler TileDropConditionChecker;

        public bool UseCursorPositionForDropPoint = false;
        
        
        public UnityAction<(GridTileCoordinate, GridTileGroup)> OnTileDropped;
        
        private void Start()
        {
            if(GridBoard == null)
            {
                GridBoard = GetComponent<GridBoard>();
            }
        }

        public void ClearSlot(int row, int col)
        {
        }

        public bool OnDrop(IDraggable draggable)
        {
            if(GridBoard == null) return false;
            GridTileDraggable gridTileDraggable = draggable as GridTileDraggable;
            if(gridTileDraggable == null) return false;

            GridTileCoordinate coord = GetRowColFromDraggablePosition(gridTileDraggable);
            int row = coord.Row;
            int col = coord.Column;
            if(!gridTileDraggable.CanBeDroppedToSlot(GridBoard, row, col)) return false;
            bool result = HandleDroppedTile(gridTileDraggable, row, col);
            if (result)
            {
                OnTileDropped?.Invoke((coord, gridTileDraggable.GridTileGroup));
            }

            return result;
        }

        public GridTileCoordinate GetRowColFromDraggablePosition(GridTileDraggable draggable)
        {
            Vector3 positionToCheck;
            positionToCheck = draggable.GetAnchorTilePosition(GridBoard);
            return GridBoard.GetRowColFromPosition(positionToCheck);

        }

       
        public virtual bool HandleDroppedTile(GridTileDraggable draggableTile, int row, int col)
        {
            if(DroppedTileHandler != null)
            {
                return DroppedTileHandler(draggableTile, row, col);
            }
            draggableTile.HandleDropToBoard(GridBoard, row, col);
            return true;
        }

        public bool CanTileDropped(GridTileDraggable draggableTile, int row, int col)
        {
            if (TileDropConditionChecker != null)
            {
                return TileDropConditionChecker(draggableTile, row, col);
            }
            return true;
        }
    }
}