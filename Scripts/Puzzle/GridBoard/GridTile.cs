using System;
using UnityEngine;

namespace Kuantech.Puzzle
{   
    [Serializable]
    public struct GridTileData
    {
        public string Id;
        public GridTile Prefab;
    }   

    public class GridTile : MonoBehaviour {
        [NonSerialized] public GridBoard ParentBoard;
        [NonSerialized] public int Row;
        [NonSerialized] public int Column; 

        public virtual void SetRowCol(int row, int col)
        {
            Row = row;
            Column = col;
        }
    }
}