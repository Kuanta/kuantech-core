using System;
using System.Collections.Generic;
using Kuantech.AI.Pathfinding;
using UnityEngine;

namespace Kuantech.Puzzle.Pathfinding
{
    [Serializable]
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

        public override bool IsPassable()
        {
            if (_parentBoard.IsTileOccupied(TileCoordinate.Row, TileCoordinate.Column, TileCoordinate.Layer))
                return false;
            return base.IsPassable();
        }

        public override List<PathNode> GetConnectedNodes()
        {
            List<PathNode> connectedNodes = new List<PathNode>();
            List<GridBoardPathNode> neighsPathNodes = _parentBoard.Get4NeighsPathNodes(Row, Column);
            if (ConnectedNodes != null)
            {
                foreach (var connected in ConnectedNodes)
                {
                    connectedNodes.Add(connected);
                }
            }
            foreach (var node in neighsPathNodes)
            {
                connectedNodes.Add(node);
            }
            return connectedNodes;
        }
    }
}