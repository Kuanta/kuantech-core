using System.Collections.Generic;
using Kuantech.Utils;
using Sirenix.Serialization;
using UnityEngine;

namespace Kuantech.Puzzle.MatchThree
{
    public class MatchFinder
    {
        public MatchThreeBoard Board;
        private HashSet<MatchThreeElement> _elementsToBeMatched;
        public MatchFinder(MatchThreeBoard parentBoard)
        {
            Board = parentBoard;
        }

        public bool FindAllMatches()
        {
            _elementsToBeMatched = new HashSet<MatchThreeElement>();
            for(int r=0;r<Board.RowCount;++r)
            {
                for(int c=0;c<Board.ColumnCount;++c)
                {
                    MatchThreeElement element = Board.GetTile(r,c) as MatchThreeElement;
                    if(element == null) continue;

                    MatchThreeElement upElement = Board.GetTile(r+1, c) as MatchThreeElement;
                    MatchThreeElement downElement = Board.GetTile(r-1, c) as MatchThreeElement;
                    MatchThreeElement leftElement = Board.GetTile(r, c-1) as MatchThreeElement;
                    MatchThreeElement rightElement = Board.GetTile(r, c+1) as MatchThreeElement;

                    if(upElement != null && downElement != null && element.IsSameType(upElement) && element.IsSameType(downElement))
                    {
                        if(!_elementsToBeMatched.Contains(upElement)) _elementsToBeMatched.Add(upElement);
                        if(!_elementsToBeMatched.Contains(downElement)) _elementsToBeMatched.Add(downElement);
                        if (!_elementsToBeMatched.Contains(element)) _elementsToBeMatched.Add(element);
                    }

                    if (leftElement != null && rightElement != null && element.IsSameType(leftElement) && element.IsSameType(rightElement))
                    {
                        if (!_elementsToBeMatched.Contains(leftElement)) _elementsToBeMatched.Add(leftElement);
                        if (!_elementsToBeMatched.Contains(rightElement)) _elementsToBeMatched.Add(rightElement);
                        if(!_elementsToBeMatched.Contains(element)) _elementsToBeMatched.Add(element);
                    }

                }
            }

            if(_elementsToBeMatched.Count == 0) return false;
            foreach (var el in _elementsToBeMatched)
            {
                if(el == null)
                {
                    Debug.LogError("Null element?");
                    continue;
                }
                el.Despawn();
            }
            _elementsToBeMatched.Clear();
            return true;
        }

        /// <summary>
        /// Searches for matches recursively around the tile
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="foundMatches"></param>
        /// <param name="checkedTiles"></param>
        public void FindMatchesAroundTile(MatchThreeElement tile, HashSet<MatchThreeElement> foundMatches, HashSet<MatchThreeElement> checkedTiles)
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