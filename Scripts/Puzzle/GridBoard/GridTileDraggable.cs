using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Puzzle
{
    public class GridTileDraggable : Draggable
    {
        public GridTileGroup GridTileGroup;
        
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
    }
}