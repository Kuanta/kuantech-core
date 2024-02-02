using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Kuantech.Puzzle.MatchThree
{
    public class MatchGroup
    {
        public enum MatchGroupShapes
        {
            None,
            Horizontal,
            Vertical,
            LShape,
        }

        public MatchGroup()
        {
            _matches = new List<HashSet<MatchThreeElement>>();
        }
        private List<HashSet<MatchThreeElement>> _matches = new List<HashSet<MatchThreeElement>>();
        public int GetMatchCount()
        {
            if (_matches == null) return 0;
            int matchCount = 0;
            for (int i = 0; i < _matches.Count; ++i)
            {
                if (_matches[0].Count > 0) matchCount++;
            }
            return matchCount;
        }

        public List<HashSet<MatchThreeElement>> GetMatches()
        {
            return _matches;
        }

        public void AddMatch(HashSet<MatchThreeElement> match)
        {
            for(int i=0;i<_matches.Count;++i)
            {
                bool hasCommonElement = false;
                int indexOfMatchWithCommonElement = 0;
                foreach(var element in _matches[i])
                {
                    if(match.Contains(element))
                    {
                        indexOfMatchWithCommonElement = i;
                        hasCommonElement = true;
                        break;
                    }
                }

                if(hasCommonElement)
                {
                    //Add the elements of match to the match with common element
                    foreach(var element in match)
                    {
                        if(!_matches[indexOfMatchWithCommonElement].Contains(element))
                        {
                            _matches[indexOfMatchWithCommonElement].Add(element);
                        }
                    }
                    return;
                }
            }

            //No common element
            _matches.Add(match);
        }

        public static MatchGroupShapes DetectMatchShape(HashSet<MatchThreeElement> match)
        {
            if(match.Count <= 3) return MatchGroupShapes.None;
            
            List<MatchThreeElement> matchList = match.ToList();
            bool sameRows = true;
            bool sameCols = true;
            int previousRow = matchList[0].Row;
            int previousCol = matchList[0].Column;
            for(int i=0;i<matchList.Count;++i)
            {
                MatchThreeElement element = matchList[i];
                sameRows = element.Row == previousRow && sameRows;
                sameCols = element.Column == previousCol && sameCols;
                previousRow = element.Row;
                previousCol = element.Column;
            }

            if(sameRows && sameCols)
            {
                Debug.LogError("This is impossible!");
                return MatchGroupShapes.None;
            }

            if(sameRows) return MatchGroupShapes.Vertical;
            if(sameCols) return MatchGroupShapes.Horizontal;
            
            return MatchGroupShapes.LShape;
        }
    }
}
