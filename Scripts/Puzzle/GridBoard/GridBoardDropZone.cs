using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Puzzle
{
    [RequireComponent(typeof(GridBoard))]
    public class GridBoardDropZone : MonoBehaviour, IDropZone
    {
        public GridBoard GridBoard;
        public delegate bool HandleDroppedTileHandler(GridTileDraggable draggableTile, int row, int col);
        public HandleDroppedTileHandler DroppedTileHandler;
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

            Vector3 anchorTilePosition = gridTileDraggable.GetAnchorTilePosition(GridBoard);
            GridTileCoordinate coord = GridBoard.GetRowColFromPosition(anchorTilePosition);
            int row = coord.Row;
            int col = coord.Column;
            if(!gridTileDraggable.CanBeDroppedToSlot(GridBoard, row, col)) return false;
            return HandleDroppedTile(gridTileDraggable, row, col);
        }

        public virtual bool HandleDroppedTile(GridTileDraggable draggableTile, int row, int col)
        {
            if(DroppedTileHandler != null)
            {
                return DroppedTileHandler(draggableTile, row, col);
            }
            draggableTile.GridTileGroup.PlaceOnBoard(GridBoard, row, col);
            Destroy(draggableTile.gameObject);
            return true;
        }

        public bool CanTileDropped(GridTileDraggable draggableTile, int row, int col)
        {
            return true;
        }
    }
}