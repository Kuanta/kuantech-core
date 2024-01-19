using System.Collections;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Puzzle.MatchThree
{
    public class MatchThreeBoard : GridBoard {

        [Header("Timing")]
        public float ElementMovementDuration = 0.2f;

        [Header("Prefabs")]
        [SerializeField] private GameObject BgTilePrefab;
        [SerializeField] private MatchThreeElement ElementPrefab;

        private MatchFinder MatchFinder;

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
                    //Create 
                    Vector3 pos = GetLocalPosition(r,c);
                    GameObject tileBg = Instantiate(BgTilePrefab);
                    tileBg.transform.SetParent(transform);
                    tileBg.transform.localPosition = pos;
                    tileBg.name = $"BGTile_{r}_{c}";
                    MatchThreeElement tile = SpawnRandomElement(pos,r,c);

                    //Prevent premade matches
                    foundElements.Clear();
                    checkedElements.Clear();
                    MatchFinder.FindMatchesAroundTile(tile, foundElements, checkedElements);
                    int iterations = 0;
                    while(!foundElements.IsNullOrEmpty())
                    {
                        tile.ChangeType();
                        foundElements.Clear();
                        checkedElements.Clear();
                        MatchFinder.FindMatchesAroundTile(tile, foundElements, checkedElements);
                        iterations++;
                        if(iterations >= 100) break;
                    }
            
                }
            }
        }

        private MatchThreeElement SpawnRandomElement(Vector3 position, int row, int col)
        {
            int elementType = Random.Range(0, ElementPrefab.Datas.Count);
            MatchThreeElement element = GameManager.Instance.Pool.GetObject(ElementPrefab.gameObject).GetComponent<MatchThreeElement>();
            element.SetElement(elementType);
            element.SetBoard(this, row, col);
            element.transform.SetParent(transform);
            element.transform.localPosition = position;
            element.name = $"Gem_{row}_{col}";
            SetTile(element, row, col);
            return element;
        }

        /// <summary>
        /// Moves an element from a tile to another
        /// </summary>
        /// <param name="element1"></param>
        /// <param name="element2"></param>
        public void MakeAMove(MatchThreeElement element1, MatchThreeElement element2)
        {
            StartCoroutine(_MakeAMoveRoutine(element1, element2));
   
        }

        private IEnumerator _MakeAMoveRoutine(MatchThreeElement element1, MatchThreeElement element2)
        {
            SwapElements(element1, element2);
            yield return new WaitForSeconds(ElementMovementDuration+0.1f);
            //Check Matches
            HashSet<MatchThreeElement> foundElements = new HashSet<MatchThreeElement>();
            HashSet<MatchThreeElement> checkedElements = new HashSet<MatchThreeElement>();
            MatchFinder.FindMatchesAroundTile(element1, foundElements, checkedElements);
            MatchFinder.FindMatchesAroundTile(element2, foundElements, checkedElements);
            if (foundElements.IsNullOrEmpty())
            {
                SwapElements(element1, element2);
                yield break;
            }

            //Handle matches
        
            foreach (var el in foundElements)
            {
                if (el == null)
                {
                    Debug.LogError("Null element?");
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

            //todo: Implement animation here
            element1.MoveTile(GetLocalPosition(element1.Row, element1.Column));
            element2.MoveTile(GetLocalPosition(element2.Row, element2.Column));

        }

    }

}