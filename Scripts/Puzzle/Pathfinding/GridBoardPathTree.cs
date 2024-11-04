using Kuantech.AI.Pathfinding;
using UnityEngine;

namespace Kuantech.Puzzle.Pathfinding
{
    /// <summary>
    /// A component for grid board that creates path nodes
    /// </summary>
    public class GridBoardPathTree : MonoBehaviour
    {
        public GridBoard ParentBoard;
        public GridBoardPathNode[,] PathNodes;

        public void CreateNodes(GridBoard parentBoard)
        {
            ParentBoard = parentBoard;
            PathNodes = new GridBoardPathNode[parentBoard.RowCount, parentBoard.ColumnCount];
            for (int r = 0; r < parentBoard.RowCount; ++r)
            {
                for (int c = 0; c < parentBoard.ColumnCount; ++c)
                {
                    GridBoardPathNode node = new GridBoardPathNode(r, c, parentBoard);
                    PathNodes[r, c] = node;
                }
            }
        }

        public GridBoardPathNode GetPathNodeAtCoordinate(GridTileCoordinate coordinate)
        {
            return GetPathNodeAtCoordinate(coordinate.Row, coordinate.Column, coordinate.Layer);
        }

        public GridBoardPathNode GetPathNodeAtCoordinate(int row, int col, int layer = 0)
        {
            if (!ParentBoard.IsCoordinateValid(row, col)) return null;
            return PathNodes[row, col];
        }
    }
}