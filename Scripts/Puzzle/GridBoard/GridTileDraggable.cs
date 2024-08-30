using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Puzzle
{
    public class GridTileDraggable : Draggable
    {
        public GridTileGroup GridTileGroup;
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
            return GridTileGroup.CanBePlacedToBoard(board, row, col);
        }
        
        /// <summary>
        /// Returns the position of the tile at local (0,0). It will be used to place the group on the board
        /// </summary>
        /// <param name="board"></param>
        /// <returns></returns>
        public Vector3 GetAnchorTilePosition(GridBoard board)
        {
            return GridTileGroup.GetAnchorTilePosition(board);
        }
        
        public override void Drag(Vector3 cursorPosition)
        {
            base.Drag(cursorPosition);

            if (!HighlightBoard) return;
            if (DropZone != null && DropZone is GridBoardDropZone gridBoardDropZone)
            {
                GridTileCoordinate coord = gridBoardDropZone.GetRowColFromDraggablePosition(this);
                
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
            Debug.LogError("Left drop zone");
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
            foreach (var pair in GridTileGroup.ChildTiles)
            {
                coordinates.Add(new GridTileCoordinate()
                {
                    Row = pair.Key.Row + coordinate.Row,
                    Column = pair.Key.Column + coordinate.Column,
                });
            }
            board.HighlightTiles(coordinates);
        }
    }
}