using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Puzzle
{
    public class GridTileDraggable : Draggable
    {
        [Header("Layer")]
        public int LayerToDrop = 0;
        [Header("Spanned Tiles")] 
        public GridTile GridTile;

        [Tooltip("If set to true, draggable will try to highlight ")]
        
        [Header("Grid Tile Group")]
        public GridTileGroup GridTileGroup;
        
        [Tooltip("If set to true, draggable will try to highlight ")]
        public bool HighlightBoard = false;

        public bool DestroyOnDrop;
        
        private GridTileCoordinate _lastCoordinate;

        public UnityAction<GridTileDraggable> OnPlacedToBoard;
        
        /// <summary>
        /// Checks if this grid tile draggable can be dropped to the grid board
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public virtual bool CanBeDroppedToSlot(GridBoard board, int row, int col)
        {
            if (GridTileGroup != null)
            {
                return GridTileGroup.CanBePlacedToBoard(board, row, col, LayerToDrop);
            }

            if (GridTile != null)
            {
                return board.CanTileBePlaced(GridTile, row, col, LayerToDrop);
            }

            return true;
        }
        
        /// <summary>
        /// Returns the position of the tile at local (0,0). It will be used to place the group on the board
        /// </summary>
        /// <param name="board"></param>
        /// <returns></returns>
        public virtual Vector3 GetAnchorTilePosition(GridBoard board)
        {
            if (GridTile == null && GridTileGroup == null) return Vector3.zero;
            if (GridTileGroup == null)
            {
                //No grid tile group
                return transform.position + GridTile.AnchorOffset;
            }
            return GridTileGroup.GetAnchorTilePosition(board);
        }
        
        public override void Drag(Vector3 cursorPosition, Vector3 cursorWorldPositionChange)
        {
            base.Drag(cursorPosition, cursorWorldPositionChange);

            if (!HighlightBoard) return;
            if (DropZone != null && DropZone is GridBoardDropZone gridBoardDropZone)
            {
                GridTileCoordinate coord = gridBoardDropZone.GetRowColFromDraggablePosition(this);
                
                //If there is a change in the coordinates, clear the highlights
                if (_lastCoordinate == null || coord.Row != _lastCoordinate.Row || coord.Column != _lastCoordinate.Column)
                {
                    if(gridBoardDropZone != null) gridBoardDropZone.GridBoard.ClearHighlightedTiles();
                }
                if (CanBeDroppedToSlot(gridBoardDropZone.GridBoard, coord.Row, coord.Column))
                {

                    HighlightBoardBacgkroundTiles(gridBoardDropZone.GridBoard, _lastCoordinate);
                }

                if (_lastCoordinate == null)
                {
                    _lastCoordinate = new GridTileCoordinate();
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

        public override void OnDragEndAsProxy()
        {
            base.OnDragEndAsProxy();
            if (DropZone != null && DropZone is GridBoardDropZone zone)
            {
                zone.GridBoard.ClearHighlightedTiles();
            }
        }
        
        public void HighlightBoardBacgkroundTiles(GridBoard board, GridTileCoordinate coordinate)
        {
            if (coordinate == null) return;
            List<GridTileCoordinate> coordinates = new List<GridTileCoordinate>();
            if (GridTileGroup != null)
            {
                foreach (var pair in GridTileGroup.ChildTiles)
                {
                    coordinates.Add(new GridTileCoordinate()
                    {
                        Row = pair.Key.Row + coordinate.Row,
                        Column = pair.Key.Column + coordinate.Column,
                    });
                }
            }
            else if(!GridTile.Coordinates.IsNullOrEmpty())
            {
                foreach (var coord in GridTile.Coordinates)
                {
                    coordinates.Add(new GridTileCoordinate()
                    {
                        Row = coord.Row + coordinate.Row,
                        Column = coord.Column + coordinate.Column,
                    });
                }
            }
            else
            {
                coordinates.Add(coordinate);
            }
    
            board.HighlightTiles(coordinates);
        }

        public virtual bool HandleDropToBoard(GridBoard board, int rowToDrop, int colToDrop)
        {
            if (GridTileGroup != null)
            {
                if (!GridTileGroup.PlaceOnBoard(board, rowToDrop, colToDrop, LayerToDrop)) return false;
            }

            if (GridTile != null)
            {
                bool result = board.SetTile(GridTile, rowToDrop, colToDrop, LayerToDrop);
                if (!result) return false;
            }
            
            //Fire the event
            OnPlacedToBoard?.Invoke(this);
            
            if (DestroyOnDrop)
            {
                Destroy(gameObject);
            }

            return true;
        }

    }
}