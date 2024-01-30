using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.Puzzle.MatchThree
{
    public class MatchGroup
    {
        public List<HashSet<MatchThreeElement>> Matches = new List<HashSet<MatchThreeElement>>();
        public int GetMatchCount()
        {
            if(Matches == null) return 0;
            int matchCount = 0;
            for(int i=0;i<Matches.Count;++i)
            {
                if(Matches[0].Count > 0) matchCount++;
            }
            return matchCount;
        }
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

        //Events
        public Action OnMove;
        public Action<(MatchThreeElementData, int)> OnCollectElement;

        public void Setup()
        {
            MatchFinder = new MatchFinder(this);
            CreateBoard();
            CreateInitialElements(true);
        }

        public override GridTile CreateExistingTile(ExistingTileInfo existingTileInfo)
        {
            GridTile tile = base.CreateExistingTile(existingTileInfo);
            if(tile is MatchThreeElement element)
            {
                element.SetBoard(this, tile.Row, tile.Column);
                element.transform.SetParent(transform);
                element.transform.localPosition = GetLocalPosition(tile.Row, tile.Column);
                element.name = $"Gem_{tile.Row}_{tile.Column}";
                SetTile(element, tile.Row, tile.Column);
                element.SetInitialized();
            }
            return tile;
        }

        /// <summary>
        /// Creates the initial elements
        /// </summary>
        public void CreateInitialElements(bool setBackgroundTiles=false)
        {
            for (int r = 0; r < RowCount; ++r)
            {
                for (int c = 0; c < ColumnCount; ++c)
                {
                    if (IsTileOccupied(r, c))
                    {
                        continue;
                    }
                    //Create 
                    Vector3 pos = GetLocalPosition(r, c);
                    if(setBackgroundTiles)
                    {
                        GameObject tileBg = Instantiate(BgTilePrefab);
                        tileBg.transform.SetParent(transform);
                        tileBg.transform.localPosition = pos;
                        tileBg.name = $"BGTile_{r}_{c}";
                    }
                    MatchThreeElement tile = SpawnRandomElement(r, c);
                    if(tile == null)
                    {
                        Debug.LogError("Why we have null?");
                    }
                    //Prevent premade matches
                    MatchGroup group = MatchFinder.FindMatchesAroundTile(tile);
                    int iterations = 0;
                    while (group.GetMatchCount() > 0)
                    {
                        ChangeTileType(tile);
                        group = MatchFinder.FindMatchesAroundTile(tile);
                        iterations++;
                        if (iterations >= 100) {
                            break;
                        }
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
            return SpawnElement(ElementDatas.GetRandomElement(), row, col);
        }

        /// <summary>
        /// Spawns an element at given row col
        /// </summary>
        /// <param name="data"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        protected virtual MatchThreeElement SpawnElement(MatchThreeElementData data, int row, int col)
        {
            MatchThreeElement element = CreateMatchThreeElement(data);
            element.SetBoard(this, row, col);
            element.transform.SetParent(transform);
            element.transform.localPosition = GetLocalPosition(row, col);
            element.name = $"Gem_{row}_{col}";
            SetTile(element, row, col);
            return element;
        }

        /// <summary>
        /// Creates an element of random type
        /// </summary>
        /// <returns></returns>
        protected virtual MatchThreeElement CreateRandomElement()
        {
            MatchThreeElementData elementType = ElementDatas.GetRandomElement();
            return CreateMatchThreeElement(elementType);
        }

        /// <summary>
        /// Creates an element of the given type
        /// </summary>
        /// <param name="elementType"></param>
        /// <returns></returns>
        protected MatchThreeElement CreateMatchThreeElement(MatchThreeElementData elementType)
        {
            MatchThreeElement element = Instantiate(ElementPrefab.gameObject).GetComponent<MatchThreeElement>();
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
                OnMoveEnd(false);
                yield break;
            }
            HandleMatchGroups(groups, false);
            PostMove();
        }

        private void PostMove()
        {
            StartCoroutine(PostMoveCo());
        }

        /// <summary>
        /// Should be called whenever a move is ended
        /// </summary>
        /// <param name="isValidMove"></param>
        private void OnMoveEnd(bool isValidMove)
        {
            _inputBlocked = false;
            if(isValidMove) OnMove?.Invoke();
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
                HandleMatchGroups(groups, true);
                PostMove();
                yield break;
            }
            OnMoveEnd(true);
        }

        protected virtual void HandleMatchGroups(MatchGroup group, bool fromPostMove)
        {
            foreach(HashSet<MatchThreeElement> g in group.Matches)
            {
                HandleMatches(g, fromPostMove);
            }
        }
        protected virtual void HandleMatches(HashSet<MatchThreeElement> matches, bool fromPostMove)
        {
            if (matches.Count <= 0) return;
            List<MatchThreeElement> matchesList = matches.ToList();
            OnCollectElement?.Invoke((matchesList[0].CurrentData, matchesList.Count));
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
                        tile.SetRowCol(r - nullCounter, c);
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
            for (int c = 0; c < ColumnCount; ++c)
            {
                for (int r = 0; r < RowCount; ++r)
                {
                    if(Tiles[r,c] == null)
                    {
                        Vector3 localPosition = GetLocalPosition(RowCount, c); //Position to above so that is aboce
                        MatchThreeElement newTile = SpawnRandomElement(r, c);
                        newTile.transform.localPosition = localPosition;
                    }
                }
            }
        }

        public void RestartBoard()
        {
            StopAllCoroutines();
            _inputBlocked = false;
            ClearBoard();
            SetExistingTiles();
            CreateInitialElements();
        }

        #region Debugging
        [Button("Find Matches Around Tile")]
        public void FindMatchesAroundTileDebug(int row, int col)
        {
            MatchThreeElement tile = GetTile(row, col) as MatchThreeElement;
            if(tile == null) return;
            MatchGroup group = MatchFinder.FindMatchesAroundTile(tile);
            Debug.LogError($"Found {group.GetMatchCount()} matches");
        }
        #endregion
    }

}