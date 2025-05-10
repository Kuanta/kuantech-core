using System.Collections.Generic;
using System.Numerics;
using Kuantech.Utils;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace Kuantech.Puzzle
{
    public class GridTileDraggableV2 : Draggable
    {
        public List<GridTileCoordinate> SpannedTiles;
        [Tooltip("transform.position + Offset gives the anchor position")]
        public Vector3 AnchorOffset = Vector3.zero;
        [Tooltip("If set to true, draggable will try to highlight ")]
        public bool HighlightBoard = false;

        private GridTileCoordinate _lastCoordinate;
        
        /// <summary>
        /// Checks if this grid tile draggable can be dropped to the grid board
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public virtual bool CanBeDroppedToSlot(GridBoard board, int row, int col)
        {
            foreach (var coord in SpannedTiles)
            {
                if (!board.IsTileValidAndEmpty(coord.Row + row, coord.Column + col)) return false;
            }

            return true;
        }
        
        public override void Drag(Vector3 cursorPosition, Vector3 cursorWorldPositionChange)
        {
            base.Drag(cursorPosition, cursorWorldPositionChange);

            if (!HighlightBoard) return;
            if (DropZone != null && DropZone is GridBoardDropZone gridBoardDropZone)
            {
                GridTileCoordinate coord = gridBoardDropZone.GridBoard.GetRowColFromPosition(GetAnchorPosition());
                
                //If there is a change in the coordinates, clear the highlights
                if (coord.Row != _lastCoordinate.Row || coord.Column != _lastCoordinate.Column)
                {
                    gridBoardDropZone.GridBoard.ClearHighlightedTiles();
                }
                if (CanBeDroppedToSlot(gridBoardDropZone.GridBoard, coord.Row, coord.Column))
                {

                    HighlightBoardBacgkroundTiles(gridBoardDropZone.GridBoard, _lastCoordinate);
                }
                _lastCoordinate.Row = coord.Row;
                _lastCoordinate.Column = coord.Column;
            }
        }
        
        public override void OnLeftDropZone(IDropZone dropZone)
        {
            base.OnLeftDropZone(dropZone);
            GridBoardDropZone gz = (dropZone as GridBoardDropZone);
            if (gz == null) return;
            gz.GridBoard.ClearHighlightedTiles();
        }
        
        public override void DragEnd()
        {
            if (DropZone != null && DropZone is GridBoardDropZone zone)
            {
                zone.GridBoard.ClearHighlightedTiles();
            }
            base.DragEnd();
        }
        
        public void HighlightBoardBacgkroundTiles(GridBoard board, GridTileCoordinate coordinate)
        {
            List<GridTileCoordinate> coordinates = new List<GridTileCoordinate>();
            foreach (var coord in SpannedTiles)
            {
                coordinates.Add(new GridTileCoordinate()
                {
                    Row = coord.Row + coordinate.Row,
                    Column = coord.Column + coordinate.Column,
                });
            }
            board.HighlightTiles(coordinates);
        }
        
        /// <summary>
        /// Position that represents local (0,0)
        /// </summary>
        /// <returns></returns>
        public virtual Vector3 GetAnchorPosition()
        {
            return transform.position - AnchorOffset;
        }
    }
}