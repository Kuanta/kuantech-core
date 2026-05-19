using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Puzzle
{
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
            if (UseCursorPositionForDropPoint)
                return GetRowColFromCursorPosition();

            if (draggable.IsCanvasElement())
            {
                if (draggable.GridTileGroup != null)
                    return GetRowColFromGroupAnchor(draggable.GridTileGroup);
                return GetRowColFromCanvasSingleTile(draggable);
            }

            Vector3 positionToCheck = draggable.GetAnchorTilePosition(GridBoard);
            return GridBoard.GetRowColFromPosition(positionToCheck);
        }

        private GridTileCoordinate GetRowColFromCursorPosition()
        {
            RectTransform boardRect = GridBoard.GetComponent<RectTransform>();
            if (boardRect == null)
                return GridBoard.GetRowColFromPosition(Vector3.zero);

            Canvas canvas = boardRect.GetComponentInParent<Canvas>();
            Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? canvas.worldCamera
                : null;

            Vector2 screenPos = DragManager.GetCursorPosition(true);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(boardRect, screenPos, cam, out Vector2 localPoint);
            Vector3 worldPos = boardRect.TransformPoint(localPoint);
            return GridBoard.GetRowColFromPosition(worldPos);
        }

        private GridTileCoordinate GetRowColFromCanvasSingleTile(GridTileDraggable draggable)
        {
            RectTransform boardRect = GridBoard.GetComponent<RectTransform>();
            if (boardRect == null)
                return GridBoard.GetRowColFromPosition(Vector3.zero);

            Canvas canvas = boardRect.GetComponentInParent<Canvas>();
            Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? canvas.worldCamera
                : null;

            Vector2 screenPos = DragManager.GetCursorPosition(true);

            // AnchorOffset is in canvas pixels; multiply by scaleFactor to convert to screen pixels
            if (draggable.GridTile != null && draggable.GridTile.AnchorOffset != Vector3.zero)
            {
                float scale = canvas != null ? canvas.scaleFactor : 1f;
                screenPos += (Vector2)draggable.GridTile.AnchorOffset * scale;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(boardRect, screenPos, cam, out Vector2 localPoint);
            Vector3 worldPos = boardRect.TransformPoint(localPoint);
            return GridBoard.GetRowColFromPosition(worldPos);
        }

        private GridTileCoordinate GetRowColFromGroupAnchor(GridTileGroup group)
        {
            RectTransform boardRect = GridBoard.GetComponent<RectTransform>();
            if (boardRect == null)
                return GridBoard.GetRowColFromPosition(Vector3.zero);

            GridTile anchorTile = null;
            foreach (var pair in group.ChildTiles)
            {
                if (pair.Key.Row == 0 && pair.Key.Column == 0)
                {
                    anchorTile = pair.Value;
                    break;
                }
            }

            if (anchorTile == null)
                return GetRowColFromCursorPosition();

            Canvas canvas = boardRect.GetComponentInParent<Canvas>();
            Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? canvas.worldCamera
                : null;

            // For SSO canvas, transform.position is already in screen pixels
            Vector2 screenPos = anchorTile.transform.position;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(boardRect, screenPos, cam, out Vector2 localPoint);
            Vector3 worldPos = boardRect.TransformPoint(localPoint);
            return GridBoard.GetRowColFromPosition(worldPos);
        }
       
        public virtual bool HandleDroppedTile(GridTileDraggable draggableTile, int row, int col)
        {
            if(DroppedTileHandler != null)
            {
                return DroppedTileHandler(draggableTile, row, col);
            }
            return draggableTile.HandleDropToBoard(GridBoard, row, col);
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