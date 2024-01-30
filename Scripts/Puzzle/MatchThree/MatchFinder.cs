using System.Collections.Generic;
using Kuantech.Utils;

namespace Kuantech.Puzzle.MatchThree
{
    public class MatchFinder
    {
        public MatchThreeBoard Board;
        private HashSet<MatchThreeElement> _groupedElements;
        public MatchFinder(MatchThreeBoard parentBoard)
        {
            Board = parentBoard;
        }

        #region Depth-First-Search
        public MatchGroup FindAllMatchesV2()
        {
            MatchGroup group = new MatchGroup();
            group.Matches = new List<HashSet<MatchThreeElement>>();
            _groupedElements = new HashSet<MatchThreeElement>();
            // Iterate through each tile in the grid
            for (int c = 0; c < Board.ColumnCount; c++)
            {
                for (int r = 0; r < Board.RowCount; r++)
                {
                    MatchThreeElement currentTile = Board.GetTile(r,c) as MatchThreeElement;
                    if(currentTile == null || _groupedElements.Contains(currentTile)) continue;

                    HashSet<MatchThreeElement> matchGroup = new HashSet<MatchThreeElement>();
                    DFS(currentTile, matchGroup);

                    // Add the match group to the list if it is not empty
                    if(!matchGroup.IsNullOrEmpty()) group.Matches.Add(matchGroup);
                }
            }

            return group;
        }

        public MatchGroup FindMatchesAroundTile(MatchThreeElement tile)
        {
            if(tile == null) return null;
            MatchGroup group = new MatchGroup();
            return group;
        }

        // Depth-First Search to find connected tiles in a match group
        private void DFS(MatchThreeElement currentTile, HashSet<MatchThreeElement> matchGroup, HashSet<MatchThreeElement> traversedTiles = null)
        {
            if (currentTile == null || _groupedElements.Contains(currentTile)) return;
            if(traversedTiles == null) traversedTiles = new HashSet<MatchThreeElement>();
            if(traversedTiles.Contains(currentTile)) return;
            traversedTiles.Add(currentTile);

            MatchThreeElement upElement = Board.GetTile(currentTile.Row + 1, currentTile.Column) as MatchThreeElement;
            MatchThreeElement downElement = Board.GetTile(currentTile.Row - 1, currentTile.Column) as MatchThreeElement;
            MatchThreeElement leftElement = Board.GetTile(currentTile.Row, currentTile.Column - 1) as MatchThreeElement;
            MatchThreeElement rightElement = Board.GetTile(currentTile.Row, currentTile.Column + 1) as MatchThreeElement;

            if (upElement != null && downElement != null && currentTile.IsSameType(upElement) && currentTile.IsSameType(downElement))
            {
                if (!matchGroup.Contains(upElement)) AddTileToGroup(upElement, matchGroup);
                if (!matchGroup.Contains(downElement)) AddTileToGroup(downElement, matchGroup);
                if (!matchGroup.Contains(currentTile)) AddTileToGroup(currentTile, matchGroup);
            }

            if (leftElement != null && rightElement != null && currentTile.IsSameType(leftElement) && currentTile.IsSameType(rightElement))
            {
                if (!matchGroup.Contains(leftElement)) AddTileToGroup(leftElement, matchGroup);
                if (!matchGroup.Contains(rightElement)) AddTileToGroup(rightElement, matchGroup);
                if (!matchGroup.Contains(currentTile)) AddTileToGroup(currentTile, matchGroup);
            }

            if(currentTile.IsSameType(upElement)) DFS(upElement, matchGroup, traversedTiles);
            if(currentTile.IsSameType(leftElement)) DFS(leftElement, matchGroup, traversedTiles);
            if(currentTile.IsSameType(rightElement)) DFS(rightElement, matchGroup, traversedTiles);
            if(currentTile.IsSameType(downElement)) DFS(downElement, matchGroup, traversedTiles);
        }

        /// <summary>
        /// Adds a tile to a match group. Also add it to the grouped tiles
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="matchGroup"></param>
        private void AddTileToGroup(MatchThreeElement tile, HashSet<MatchThreeElement> matchGroup)
        {
            if(!matchGroup.Contains(tile)) matchGroup.Add(tile);
            if (!_groupedElements.Contains(tile)) _groupedElements.Add(tile);

        }
        #endregion

        /// <summary>
        /// Searches for matches recursively around the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="foundMatches"></param>
        /// <param name="checkedTiles"></param>
        public void FindMatchesAroundTile(MatchThreeElement tile, HashSet<MatchThreeElement> foundMatches=null, HashSet<MatchThreeElement> checkedTiles=null)
        {
            if(tile == null || !checkedTiles.IsNullOrEmpty() && checkedTiles.Contains(tile)) return;
            if(checkedTiles == null) checkedTiles = new HashSet<MatchThreeElement>();
            if(foundMatches == null) foundMatches = new HashSet<MatchThreeElement>();
            checkedTiles.Add(tile);
            int r = tile.Row;
            int c = tile.Column;

            MatchThreeElement upElement = Board.GetTile(r + 1, c) as MatchThreeElement;
            MatchThreeElement downElement = Board.GetTile(r - 1, c) as MatchThreeElement;
            MatchThreeElement leftElement = Board.GetTile(r, c - 1) as MatchThreeElement;
            MatchThreeElement rightElement = Board.GetTile(r, c + 1) as MatchThreeElement;


            if (upElement != null && downElement != null && tile.IsSameType(upElement) && tile.IsSameType(downElement))
            {
                if (!foundMatches.Contains(upElement)) foundMatches.Add(upElement);
                if (!foundMatches.Contains(downElement)) foundMatches.Add(downElement);
                if (!foundMatches.Contains(tile)) foundMatches.Add(tile);
            }

            if (leftElement != null && rightElement != null && tile.IsSameType(leftElement) && tile.IsSameType(rightElement))
            {
                if (!foundMatches.Contains(leftElement)) foundMatches.Add(leftElement);
                if (!foundMatches.Contains(rightElement)) foundMatches.Add(rightElement);
                if (!foundMatches.Contains(tile)) foundMatches.Add(tile);
            }


            CheckIfNeighTileShouldBeChecked(tile, upElement, foundMatches, checkedTiles);
            CheckIfNeighTileShouldBeChecked(tile, leftElement, foundMatches, checkedTiles);
            CheckIfNeighTileShouldBeChecked(tile, downElement, foundMatches, checkedTiles);
            CheckIfNeighTileShouldBeChecked(tile, rightElement, foundMatches, checkedTiles);
        }

        private void CheckIfNeighTileShouldBeChecked(MatchThreeElement tile, MatchThreeElement neighTile, HashSet<MatchThreeElement> foundMatches = null, HashSet<MatchThreeElement> checkedTiles = null)
        {
            if(neighTile == null || !tile.IsSameType(neighTile)) return;
            if(checkedTiles != null && checkedTiles.Contains(neighTile)) return;
            FindMatchesAroundTile(neighTile, foundMatches, checkedTiles);
        }
    }
}