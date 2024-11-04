using System.Collections.Generic;
using Kuantech.Utils;

namespace Kuantech.Puzzle.MatchThree
{
    public class MatchFinder
    {
        public MatchThreeBoard Board;
        public MatchFinder(MatchThreeBoard parentBoard)
        {
            Board = parentBoard;
        }

        #region Depth-First-Search
        public MatchGroup FindAllMatchesV2(int layer=0)
        {
            MatchGroup group = new MatchGroup();
    
            HashSet<MatchThreeElement> matchedElements = new HashSet<MatchThreeElement>();
            // Iterate through each tile in the grid
            for (int c = 0; c < Board.ColumnCount; c++)
            {
                for (int r = 0; r < Board.RowCount; r++)
                {
                    MatchThreeElement currentTile = Board.GetTile(r,c, layer) as MatchThreeElement;
                    if(currentTile == null) continue;

                    HashSet<MatchThreeElement> matchGroup = new HashSet<MatchThreeElement>();
                    DFS(currentTile, matchGroup, traversedTiles:null, groupedTiles:matchedElements);

                    // Add the match group to the list if it is not empty
                    if(!matchGroup.IsNullOrEmpty()) group.AddMatch(matchGroup);
                }
            }

            return group;
        }

        public MatchGroup FindMatchesAroundTile(MatchThreeElement tile, MatchGroup group=null)
        {
            if(tile == null) return null;
            if(group == null)
            {
                group = new MatchGroup();
            }
            HashSet<MatchThreeElement> matches = new HashSet<MatchThreeElement>();
            DFS(tile, matches);
            if(matches.Count >= 3)
            {
                group.AddMatch(matches);
            }
            return group;
        }

        // Depth-First Search to find connected tiles in a match group
        private void DFS(MatchThreeElement currentTile, HashSet<MatchThreeElement> matchGroup, HashSet<MatchThreeElement> traversedTiles = null, HashSet<MatchThreeElement> groupedTiles = null)
        {
            if (currentTile == null) return;
            if(groupedTiles == null) groupedTiles = new HashSet<MatchThreeElement>();
            if(traversedTiles == null) traversedTiles = new HashSet<MatchThreeElement>();
            if(traversedTiles.Contains(currentTile)) return;
            traversedTiles.Add(currentTile);

            MatchThreeElement upElement = Board.GetTile(currentTile.AnchorRow + 1, currentTile.AnchorColumn, currentTile.AnchorLayer) as MatchThreeElement;
            MatchThreeElement downElement = Board.GetTile(currentTile.AnchorRow - 1, currentTile.AnchorColumn, currentTile.AnchorLayer) as MatchThreeElement;
            MatchThreeElement leftElement = Board.GetTile(currentTile.AnchorRow, currentTile.AnchorColumn - 1, currentTile.AnchorLayer) as MatchThreeElement;
            MatchThreeElement rightElement = Board.GetTile(currentTile.AnchorRow, currentTile.AnchorColumn + 1, currentTile.AnchorLayer) as MatchThreeElement;

            if (upElement != null && downElement != null && currentTile.IsSameType(upElement) && currentTile.IsSameType(downElement))
            {
                if (!matchGroup.Contains(upElement)) AddTileToGroup(upElement, matchGroup, groupedTiles);
                if (!matchGroup.Contains(downElement)) AddTileToGroup(downElement, matchGroup, groupedTiles);
                if (!matchGroup.Contains(currentTile)) AddTileToGroup(currentTile, matchGroup, groupedTiles);
            }

            if (leftElement != null && rightElement != null && currentTile.IsSameType(leftElement) && currentTile.IsSameType(rightElement))
            {
                if (!matchGroup.Contains(leftElement)) AddTileToGroup(leftElement, matchGroup, groupedTiles);
                if (!matchGroup.Contains(rightElement)) AddTileToGroup(rightElement, matchGroup, groupedTiles);
                if (!matchGroup.Contains(currentTile)) AddTileToGroup(currentTile, matchGroup, groupedTiles);
            }

            if(currentTile.IsSameType(upElement)) DFS(upElement, matchGroup, traversedTiles, groupedTiles);
            if(currentTile.IsSameType(leftElement)) DFS(leftElement, matchGroup, traversedTiles, groupedTiles);
            if(currentTile.IsSameType(rightElement)) DFS(rightElement, matchGroup, traversedTiles, groupedTiles);
            if(currentTile.IsSameType(downElement)) DFS(downElement, matchGroup, traversedTiles, groupedTiles);
        }

        /// <summary>
        /// Adds a tile to a match group. Also add it to the grouped tiles
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="matchGroup"></param>
        private void AddTileToGroup(MatchThreeElement tile, HashSet<MatchThreeElement> matchGroup, HashSet<MatchThreeElement> groupedTiles)
        {
            if(!matchGroup.Contains(tile)) matchGroup.Add(tile);
            if (!groupedTiles.Contains(tile)) groupedTiles.Add(tile);
        }
        #endregion

    //     /// <summary>
    //     /// Searches for matches recursively around the tile
    //     /// </summary>
    //     /// <param name="tile"></param>
    //     /// <param name="foundMatches"></param>
    //     /// <param name="checkedTiles"></param>
    //     public void FindMatchesAroundTile(MatchThreeElement tile, HashSet<MatchThreeElement> foundMatches=null, HashSet<MatchThreeElement> checkedTiles=null)
    //     {
    //         if(tile == null || !checkedTiles.IsNullOrEmpty() && checkedTiles.Contains(tile)) return;
    //         if(checkedTiles == null) checkedTiles = new HashSet<MatchThreeElement>();
    //         if(foundMatches == null) foundMatches = new HashSet<MatchThreeElement>();
    //         checkedTiles.Add(tile);
    //         int r = tile.Row;
    //         int c = tile.Column;

    //         MatchThreeElement upElement = Board.GetTile(r + 1, c) as MatchThreeElement;
    //         MatchThreeElement downElement = Board.GetTile(r - 1, c) as MatchThreeElement;
    //         MatchThreeElement leftElement = Board.GetTile(r, c - 1) as MatchThreeElement;
    //         MatchThreeElement rightElement = Board.GetTile(r, c + 1) as MatchThreeElement;


    //         if (upElement != null && downElement != null && tile.IsSameType(upElement) && tile.IsSameType(downElement))
    //         {
    //             if (!foundMatches.Contains(upElement)) foundMatches.Add(upElement);
    //             if (!foundMatches.Contains(downElement)) foundMatches.Add(downElement);
    //             if (!foundMatches.Contains(tile)) foundMatches.Add(tile);
    //         }

    //         if (leftElement != null && rightElement != null && tile.IsSameType(leftElement) && tile.IsSameType(rightElement))
    //         {
    //             if (!foundMatches.Contains(leftElement)) foundMatches.Add(leftElement);
    //             if (!foundMatches.Contains(rightElement)) foundMatches.Add(rightElement);
    //             if (!foundMatches.Contains(tile)) foundMatches.Add(tile);
    //         }


    //         CheckIfNeighTileShouldBeChecked(tile, upElement, foundMatches, checkedTiles);
    //         CheckIfNeighTileShouldBeChecked(tile, leftElement, foundMatches, checkedTiles);
    //         CheckIfNeighTileShouldBeChecked(tile, downElement, foundMatches, checkedTiles);
    //         CheckIfNeighTileShouldBeChecked(tile, rightElement, foundMatches, checkedTiles);
    //     }

    //     private void CheckIfNeighTileShouldBeChecked(MatchThreeElement tile, MatchThreeElement neighTile, HashSet<MatchThreeElement> foundMatches = null, HashSet<MatchThreeElement> checkedTiles = null)
    //     {
    //         if(neighTile == null || !tile.IsSameType(neighTile)) return;
    //         if(checkedTiles != null && checkedTiles.Contains(neighTile)) return;
    //         FindMatchesAroundTile(neighTile, foundMatches, checkedTiles);
    //     }
    }
}