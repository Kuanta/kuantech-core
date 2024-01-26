using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Puzzle.MatchThree
{
    public class MatchGroup
    {
        public List<HashSet<MatchThreeElement>> Matches;
    }

    public class MatchThreeBoard : GridBoard {
        
        [Header("Available Elements")]
        public List<MatchThreeElementData> ElementDatas;

        [Header("Timing")]
        public float ElementMovementDuration = 0.2f;
        public float TileSpeed = 5.0f;
        public float SlideWaitDelay = 0.25f;
        public float MatchCheckAfterSlideDelay = 0.5f;

        [Header("Prefabs")]
        [SerializeField] private GameObject BgTilePrefab;
        [SerializeField] private MatchThreeElement ElementPrefab;

        private MatchFinder MatchFinder;
        private bool _inputBlocked = false;

        private void Start()
        {
            Setup();
        }
        public void Setup()
        {
            MatchFinder = new MatchFinder(this);
            CreateBoard();
            HashSet<MatchThreeElement> foundElements = new HashSet<MatchThreeElement>();
            HashSet<MatchThreeElement> checkedElements = new HashSet<MatchThreeElement>();
            for (int r = 0;r<RowCount;++r)
            {
                for(int c=0;c<ColumnCount;++c)
                {
                    if(IsTileOccupied(r,c)) continue;
                    //Create 
                    Vector3 pos = GetLocalPosition(r,c);
                    GameObject tileBg = Instantiate(BgTilePrefab);
                    tileBg.transform.SetParent(transform);
                    tileBg.transform.localPosition = pos;
                    tileBg.name = $"BGTile_{r}_{c}";
                    MatchThreeElement tile = SpawnRandomElement(r,c);

                    //Prevent premade matches
                    foundElements.Clear();
                    checkedElements.Clear();
                    MatchFinder.FindMatchesAroundTile(tile, foundElements, checkedElements);
                    int iterations = 0;
                    while(!foundElements.IsNullOrEmpty())
                    {
                        ChangeTileType(tile);
                        foundElements.Clear();
                        checkedElements.Clear();
                        MatchFinder.FindMatchesAroundTile(tile, foundElements, checkedElements);
                        iterations++;
                        if(iterations >= 100) break;
                    }
                }
            }
        }

        /// <summary>
        /// Spawns a random element in given row and col
        /// </summary>
        /// <param name="position"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        protected virtual MatchThreeElement SpawnRandomElement(int row, int col)
        {
            MatchThreeElement element = CreateRandomElement();
            element.SetBoard(this, row, col);
            element.transform.SetParent(transform);
            element.transform.localPosition = GetLocalPosition(row, col);
            element.name = $"Gem_{row}_{col}";
            SetTile(element, row, col);
            return element;
        }

        protected virtual MatchThreeElement CreateRandomElement()
        {
            MatchThreeElementData elementType = ElementDatas.GetRandomElement();
            MatchThreeElement element = GameManager.Instance.Pool.GetObject(ElementPrefab.gameObject).GetComponent<MatchThreeElement>();
            element.SetElementData(elementType);
            return element;
        }

        protected virtual void ChangeTileType(MatchThreeElement tile)
        {
            ElementDatas.Shuffle();
            for(int i=0;i<ElementDatas.Count;++i)
            {
                if(!tile.CurrentData.IsSameType(ElementDatas[i]))
                {
                    tile.SetElementData(ElementDatas[i]);
                    return;
                }
            } 
        }

        /// <summary>
        /// Moves an element from a tile to another
        /// </summary>
        /// <param name="element1"></param>
        /// <param name="element2"></param>
        public void MakeAMove(MatchThreeElement element1, MatchThreeElement element2)
        {
            if(_inputBlocked) return;
            _inputBlocked = true;
            StartCoroutine(_MakeAMoveRoutine(element1, element2));
        }

        private IEnumerator _MakeAMoveRoutine(MatchThreeElement element1, MatchThreeElement element2)
        {
            SwapElements(element1, element2);
            yield return new WaitForSeconds(ElementMovementDuration+0.1f);

            MatchGroup groups = MatchFinder.FindAllMatchesV2();
            if(groups.Matches.IsNullOrEmpty())
            {
                SwapElements(element1, element2);
                yield return new WaitForSeconds(ElementMovementDuration);
                _inputBlocked = false;
                yield break;
            }
            HandleMatchGroups(groups);
            PostMove();
        }

        private void PostMove()
        {
            StartCoroutine(PostMoveCo());
        }

        private IEnumerator PostMoveCo()
        {
            yield return new WaitForSeconds(SlideWaitDelay);
            DropRows();
            RefillBoard();
            yield return new WaitForSeconds(MatchCheckAfterSlideDelay);
            MatchGroup groups = MatchFinder.FindAllMatchesV2();
            if (!groups.Matches.IsNullOrEmpty())
            {
                HandleMatchGroups(groups);
                PostMove();
                yield break;
            }
            _inputBlocked = false;
        }

        private void HandleMatchGroups(MatchGroup group)
        {
            foreach(HashSet<MatchThreeElement> g in group.Matches)
            {
                HandleMatches(g);
            }
        }
        private void HandleMatches(HashSet<MatchThreeElement> matches)
        {
            List<MatchThreeElement> listMathces = matches.ToList();
            Debug.LogError("Found group of:" + listMathces.ToList()[0].CurrentData.Name + " with a count of:"+ listMathces.Count);
            foreach (var el in matches)
            {
                if (el == null)
                {
                    continue;
                }
                Tiles[el.Row, el.Column] = null;
                el.Despawn();
            }
        }

        public void SwapElements(MatchThreeElement element1, MatchThreeElement element2)
        {
            Vector2Int element1Position = new Vector2Int(element1.Column, element1.Row); 
            Vector2Int element2Position = new Vector2Int(element2.Column, element2.Row);
            
            //Swap references
            Tiles[element1Position.y, element1Position.x] = element2;
            Tiles[element2Position.y, element2Position.x] = element1;
            
            //Swap Row & Col fields 
            element1.Row = element2Position.y;
            element1.Column = element2Position.x;
            element2.Row = element1Position.y;
            element2.Column = element1Position.x;

            element1.SetRowCol(element2Position.y, element2Position.x);
            element2.SetRowCol(element1Position.y, element1Position.x);
        }

        /// <summary>
        /// Drops the rows after a move
        /// </summary>
        private void DropRows()
        {
            int nullCounter;
            for(int c = 0; c < ColumnCount; ++c)
            {
                nullCounter = 0;
                for(int r=0;r<RowCount;++r)
                {
                    if(Tiles[r,c] == null)
                    {
                        nullCounter++;
                    }else if(nullCounter > 0)
                    {
                        GridTile tile = Tiles[r,c];
                        tile.SetRowCol(tile.Row - nullCounter, tile.Column);
                        Tiles[r-nullCounter,c] = tile;
                        Tiles[r,c] = null;
                    }
                }
            }
        }

        /// <summary>
        /// Refills the board after a move
        /// </summary>
        private void RefillBoard()
        {
            int currentIndex;
            for (int c = 0; c < ColumnCount; ++c)
            {
                currentIndex = 0;
                for (int r = 0; r < RowCount; ++r)
                {
                    if(Tiles[r,c] == null)
                    {
                        Vector3 localPosition = GetLocalPosition(RowCount + currentIndex, c); //Position to above so that is aboce
                        MatchThreeElement newTile = SpawnRandomElement(r, c);
                        newTile.transform.localPosition = localPosition;
                    }
                }
            }
        }
    }

}