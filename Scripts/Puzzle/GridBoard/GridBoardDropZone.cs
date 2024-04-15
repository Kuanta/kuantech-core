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
            if(gridTileDraggable == null || gridTileDraggable.AnchorGridTile == null) return false;
            Camera mainCamera = DragManager.GetContext<DragManager>().MainCamera;
            Ray ray = mainCamera.ScreenPointToRay(draggable.GetCursorPosition()); //todo: 
            
            Vector3 positionOnBoard = GridBoard.GetPointOnPlane(ray);
            int row, col;
            GridBoard.GetRowColFromPointOnBoard(positionOnBoard, out row, out col);
            if(!gridTileDraggable.CanBeDroppedToSlot(row, col)) return false;
            return HandleDroppedTile(gridTileDraggable, row, col);
        }

        public virtual bool HandleDroppedTile(GridTileDraggable draggableTile, int row, int col)
        {
            //GridTile tile = draggableTile.AnchorGridTile;
            if(DroppedTileHandler != null)
            {
                return DroppedTileHandler(draggableTile, row, col);
            }
            return GridBoard.MoveTile(draggableTile.AnchorGridTile, row, col);
        }

        public bool CanTileDropped(GridTileDraggable draggableTile, int row, int col)
        {
            return true;
        }
    }
}