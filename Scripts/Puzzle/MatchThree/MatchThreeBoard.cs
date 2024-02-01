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

        private Dictionary<int, int> _highestObstacleIndexInRow;
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
                    PreventMatchesForTile(tile);
                }
            }
        }

        /// <summary>
        /// Tries to prevent a match for the given tile by changing its type until no match is created for it.
        /// </summary>
        /// <param name="tile"></param>
        private void PreventMatchesForTile(MatchThreeElement tile)
        {
            //Prevent premade matches
            MatchGroup group = MatchFinder.FindMatchesAroundTile(tile);
            int iterations = 0;
            while (group.GetMatchCount() > 0)
            {
                ChangeTileType(tile);
                group = MatchFinder.FindMatchesAroundTile(tile);
                iterations++;
                if (iterations >= 100)
                {
                    break;
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
            yield return StartCoroutine(WaitTileMovement());

            MatchGroup groups = MatchFinder.FindAllMatchesV2();
            if(groups.Matches.IsNullOrEmpty())
            {
                SwapElements(element1, element2);
                yield return StartCoroutine(WaitTileMovement());
                OnMoveEnd(false);
                yield break;
            }
            HandleMatchGroups(groups, false);
            PostMove();
        }

        private IEnumerator WaitTileMovement()
        {
            while(AreTilesMoving())
            {
                yield return null;
            }
        }

        private bool AreTilesMoving()
        {
            //todo: This is temporary. Find an optimized way 
            for(int r=0;r<RowCount;++r)
            {
                for(int c=0; c<ColumnCount;++c)
                {
                    MatchThreeElement tile = GetMatchThreeElement(r,c);
                    if(tile == null) continue;
                    if(tile.IsMoving()) return true;
                }
            }
            return false;
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
            int counter = 0;
            while (DropTiles())
            {
                counter++;
                if(counter > 10)
                {
                    yield return new WaitForEndOfFrame();
                    counter = 0;
                }
            }

            yield return StartCoroutine(WaitTileMovement());
            MatchGroup groups = MatchFinder.FindAllMatchesV2();
            if (!groups.Matches.IsNullOrEmpty())
            {
                HandleMatchGroups(groups, true);
                PostMove();
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
            if(matches.Count < 3)
            {
                Debug.LogError("Oh no!");
            }
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

            element1.MoveToRowCol(element2Position.y, element2Position.x);
            element2.MoveToRowCol(element1Position.y, element1Position.x);
        }
        #region Post Move Tile Movements
        private HashSet<MatchThreeElement> _movedTiles;
        /// <summary>
        /// Drops the rows after a move
        /// </summary>
        private bool DropTiles()
        {
            bool madeValidMove = false;
            if(_movedTiles == null)
            {
                _movedTiles = new HashSet<MatchThreeElement>();
            }else{
                _movedTiles.Clear();
            }
            _highestObstacleIndexInRow = new Dictionary<int, int>();
            //Drop directly beow
            for(int c = 0; c < ColumnCount; ++c)
            {
                for(int r=0;r<RowCount;++r)
                {
                    MatchThreeElement element = GetTile(r,c) as MatchThreeElement;
                    if(element != null && element.CanBeMoved && !_movedTiles.Contains(element))                   
                    {
                        //Check below
                        int rowBelow;
                        rowBelow = GetDropRow(r, c);
                        if (rowBelow >= 0 && rowBelow < r)
                        {
                            Tiles[r, c] = null;
                            element.MoveToRowCol(rowBelow, c);
                            Tiles[rowBelow, c] = element;
                            madeValidMove = true;
                            _movedTiles.Add(element);
                        }
                    }else if(element != null && !element.CanBeMoved)
                    {
                        if(!_highestObstacleIndexInRow.ContainsKey(c))
                        {
                            _highestObstacleIndexInRow[c] = r;
                        }else{
                            _highestObstacleIndexInRow[c] = Mathf.Max(_highestObstacleIndexInRow[c], r);
                        }
                    }
                }
            }

            //Drop diagonal sideways
            for (int c = 0; c < ColumnCount; ++c)
            {
                for (int r = 0; r < RowCount; ++r)
                {
                    MatchThreeElement element = GetTile(r, c) as MatchThreeElement;
                    if (element != null && element.CanBeMoved && !_movedTiles.Contains(element))
                    {
                        //Check below
                        int rowBelow;
                        int colToCheck = c-1;
                        rowBelow = GetDropRow(r, colToCheck);

                        //Can we shift to left diagonal
                        if(rowBelow < 0 || !CanShiftToColumn(rowBelow, colToCheck) || !IsTileObstacle(r, colToCheck))
                        {
                            //Check right diagonal
                            colToCheck = c+1;
                            rowBelow = GetDropRow(r,colToCheck);
                        }
                        if (rowBelow >= 0 && rowBelow < r && 
                            CanShiftToColumn(rowBelow, colToCheck) &&  //Check if there a blocker above
                            IsTileObstacle(r, colToCheck)) //Check if there is a blocker sideways
                        {
                            Tiles[r, c] = null;
                            element.MoveToRowCol(rowBelow-1, colToCheck); //First, move to direct diagonal
                            element.MoveToRowCol(rowBelow, colToCheck); //...then drop
                            Tiles[rowBelow, colToCheck] = element;
                            madeValidMove = true;
                            _movedTiles.Add(element);
                        }
                    }
                }
            }

            //Shift sideways
            for (int r = 0; r < RowCount; ++r)
            {
                for (int c = 0; c < ColumnCount; ++c)
                {
                    MatchThreeElement element = GetTile(r, c) as MatchThreeElement;
                    if (element != null && element.CanBeMoved && !_movedTiles.Contains(element))
                    {
                        int colToShift = c;
                        if(IsCoordinateValid(r, c-1) && GetTile(r, c-1) == null && CanShiftToColumn(r, c-1))
                        {
                            colToShift = c-1;
                        }else if(IsCoordinateValid(r, c+1) && GetTile(r, c+1) == null && CanShiftToColumn(r, c+1))
                        {
                            colToShift = c+1;
                        }

                        if(colToShift != c)
                        {
                            Tiles[r, c] = null;
                            element.MoveToRowCol(r, colToShift); //...then drop
                            Tiles[r, colToShift] = element;
                            madeValidMove = true;
                            _movedTiles.Add(element);
                        }
                    }
                }
            }
            RefillBoard();
            return madeValidMove;
        }
        
        /// <summary>
        /// During diagonal and sideways shift, make sure that the above of the row is blocked.
        /// Otherwise, let the filling to the RefillTiles method
        /// </summary>
        /// <param name="startingRow"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        private bool CanShiftToColumn(int startingRow, int column)
        {
            if (_highestObstacleIndexInRow == null || !_highestObstacleIndexInRow.ContainsKey(column)) 
            {
                //There is no obstacles
                return false;
            }
            int highestObstaclesInColumn = _highestObstacleIndexInRow[column];
            return highestObstaclesInColumn > startingRow;
        }

        private int GetDropRow(int startingRow, int col)
        {
            if(!IsCoordinateValid(startingRow, col))
            {
                return -1;
            }
            int lastNullRow = startingRow;
            for(int i=startingRow-1;i>=0;--i)
            {
                MatchThreeElement element = GetMatchThreeElement(i, col);
                if(element == null)
                {
                    lastNullRow = i;
                }else if(!element.CanBeMoved)
                {
                    return lastNullRow;
                }
            }
            return lastNullRow;
        }

        /// <summary>
        /// Refills the board after a move
        /// </summary>
        private void RefillBoard()
        {
            for (int c = 0; c < ColumnCount; ++c)
            {
                int lowestRowPossible = RowCount; //An invalid tile
                for (int r = RowCount-1; r >= 0; --r)
                {
                    MatchThreeElement m3Element = GetMatchThreeElement(r, c);
                    if (m3Element == null)
                    {
                        lowestRowPossible = r;
                    }else{
                        break;
                    }
                }
                for (int r = lowestRowPossible; r < RowCount; ++r)
                {
                    if (Tiles[r, c] == null && IsCoordinateValid(r,c))
                    {
                        Vector3 localPosition = GetLocalPosition(RowCount, c); //Position to above so that is aboce
                        MatchThreeElement newTile = SpawnRandomElement(r, c);
                        newTile.transform.localPosition = localPosition;
                        newTile.MoveToRowCol(r, c);
                        //Prevent creating a match
                        PreventMatchesForTile(newTile); //todo(match3): This could be benefitial for certain game types. Discuss.
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// Checks if tile at row, col is an obstacle, a non movable tile
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public bool IsTileObstacle(int row, int col)
        {
            if(!IsCoordinateValid(row, col)) return false;
            MatchThreeElement element = GetMatchThreeElement(row, col);
            if(element == null) return false;
            return !element.CanBeMoved;
        }

        public MatchThreeElement GetMatchThreeElement(int row, int col)
        {
            return GetTile(row, col) as MatchThreeElement;
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