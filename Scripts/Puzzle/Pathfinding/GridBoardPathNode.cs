using Kuantech.AI.Pathfinding;
using UnityEngine;

namespace Kuantech.Puzzle.Pathfinding
{
    public class GridBoardPathNode : PathNode
    {
        public GridTileCoordinate TileCoordinate;
        public int Row {
            get => TileCoordinate.Row;
            private set => TileCoordinate.Row = value;
        }
        public int Column {
            get => TileCoordinate.Column;
            private set => TileCoordinate.Column = value;
        }

        private GridBoard _parentBoard;
        public GridBoardPathNode(int row, int column, GridBoard board)
        {
            TileCoordinate = new GridTileCoordinate()
            {
                Row = row,
                Column = column,
            };
            _parentBoard = board;
        }

        public override Vector3 GetPosition()
        {
            return _parentBoard.GetGlobalPosition(Row, Column);
        }
    }
}