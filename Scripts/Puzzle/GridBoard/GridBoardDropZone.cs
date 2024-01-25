using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Puzzle
{
    [RequireComponent(typeof(GridBoard))]
    public class GridBoardDropZone : MonoBehaviour, IDropZone
    {
        public GridBoard GridBoard;
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
            if(gridTileDraggable == null || gridTileDraggable.GridTile == null) return false;
            Camera mainCamera = DragManager.GetContext<DragManager>().MainCamera;
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition); //todo: 
            
            Vector3 positionOnBoard = GridBoard.GetPointOnPlane(ray);
            int row, col;
            GridBoard.GetRowColFromPointOnBoard(positionOnBoard, out row, out col);

            return GridBoard.MoveTile(gridTileDraggable.GridTile, row, col);
        }
    }
}