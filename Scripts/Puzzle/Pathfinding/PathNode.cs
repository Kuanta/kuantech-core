using UnityEngine;

namespace Kuantech.Puzzle.Pathfinding
{
    public class PathNode
    {
        public float GCost = float.MaxValue;
        public float HCost;
        public float FCost => GCost + HCost;
        public float TraverseCost = 0f;
        public PathNode ParentNode = null;
        
        /// <summary>
        /// Returns the position of the node
        /// </summary>
        /// <returns></returns>
        public virtual Vector3 GetPosition()
        {
            return Vector3.zero;
        }
    }

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