using System;
using System.Collections.Generic;

namespace Kuantech.Puzzle.Pathfinding
{
    public class GridBoardPathFinder
    {
        //public GridBoard GridBoard;
        private Dictionary<(int, int), GridBoardPathNode> _pathNodes = new Dictionary<(int, int), GridBoardPathNode>(); 
        
        public List<PathNode> GetShortestPath(GridBoard board, GridTileCoordinate start, GridTileCoordinate end)
        {
            if(_pathNodes != null) _pathNodes.Clear();
            
            GridBoardPathNode startNode = GetPathNode(board, start.Row, start.Column);
            GridBoardPathNode endNode = GetPathNode(board, end.Row, end.Column);

            startNode.GCost = 0;
            startNode.HCost = CalcualteHeuristicCost(startNode, endNode);
            //Apply A* Here
            List<GridBoardPathNode> openList = new List<GridBoardPathNode>(){startNode};
            List<GridBoardPathNode> closedList = new List<GridBoardPathNode>();
            
            
            while (openList.Count > 0)
            {
                GridBoardPathNode currentNode = openList[0];
                for (int i = 1; i < openList.Count; i++)
                {
                    if (openList[i].FCost < currentNode.FCost ||
                        (Math.Abs(openList[i].FCost - currentNode.FCost) < 1E-6 && openList[i].HCost < currentNode.HCost))
                    {
                        currentNode = openList[i];
                    }
                }
                openList.Remove(currentNode);
                closedList.Add(currentNode);

                if (currentNode == endNode)
                {
                    return RetracePath(startNode, endNode);
                }
                
                //Get neighbours
                foreach (GridBoardPathNode neighNode in GetNeighbouringNodes(board, currentNode))
                {
                    if (closedList.Contains(neighNode)) continue;
                    float newCostToNeighbor = currentNode.GCost + GetDistance(currentNode, neighNode);
                    if (newCostToNeighbor < neighNode.GCost || !openList.Contains(neighNode))
                    {
                        neighNode.GCost = newCostToNeighbor;
                        neighNode.HCost = CalcualteHeuristicCost(neighNode, endNode);
                        neighNode.ParentNode = currentNode;

                        if (!openList.Contains(neighNode))
                        {
                            openList.Add(neighNode);
                        }
                    }
                }
            }
            return new List<PathNode>();
        }
        
        /// <summary>
        /// Gets the path node at given row col
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        private GridBoardPathNode GetPathNode(GridBoard board, int row, int col)
        {
            (int, int) key = (row, col);
            if (_pathNodes == null) _pathNodes = new Dictionary<(int, int), GridBoardPathNode>();
            if (_pathNodes.ContainsKey(key)) return _pathNodes[key];
            
            //Create the node
            GridBoardPathNode node = new GridBoardPathNode(row, col, board);
            _pathNodes[key] = node;
            return node;
        }
        
        /// <summary>
        /// Gets the H cost for a given nodd using the manhattan distance
        /// </summary>
        /// <param name="node">Node to calculate H cost for</param>
        /// <param name="endNode">End node</param>
        /// <returns></returns>
        private float CalcualteHeuristicCost(GridBoardPathNode node, GridBoardPathNode endNode)
        {
            return Math.Abs(node.Row - endNode.Row) + Math.Abs(node.Column - endNode.Column);
        }
        
        /// <summary>
        /// Gets the cost of going from nodeA to nodeB
        /// </summary>
        /// <param name="nodeA"></param>
        /// <param name="nodeB"></param>
        /// <returns></returns>
        private float GetDistance(GridBoardPathNode nodeA, GridBoardPathNode nodeB)
        {
            return Math.Abs(nodeA.Row - nodeB.Row) + Math.Abs(nodeA.Column - nodeB.Column) + nodeB.TraverseCost;
        }
        List<PathNode> RetracePath(GridBoardPathNode startNode, GridBoardPathNode endNode)
        {
            List<PathNode> path = new List<PathNode>();
            GridBoardPathNode currentNode = endNode;
            
            while (currentNode != startNode)
            {
                path.Add(currentNode);
                currentNode = currentNode.ParentNode as GridBoardPathNode;
            }
            path.Add(startNode);
            path.Reverse();
            return path;
        }
        
        private List<GridBoardPathNode> GetNeighbouringNodes(GridBoard board, GridBoardPathNode node)
        {
            //List<GridTile> neighTiles = GridBoard.Get4Neighs(node.Row, node.Column);
            List<GridBoardPathNode> neighNodes = new List<GridBoardPathNode>();
            for (int i = -1; i < 2; ++i)
            {
                for (int j = -1; j < 2; ++j)
                {
                    int row = node.Row + i;
                    int col = node.Column + j;
                    if(i==j || (i != 0 && j != 0)) continue; //4 neighs condition
                    
                    //Check if occupied
                    if (board.IsTileOccupied(row, col) || !board.IsCoordinateValid(row, col)) continue;
                    
                    //Add neigh node
                    neighNodes.Add(GetPathNode(board, row, col));
                }
            }
            return neighNodes;
        }
    }
  
}