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
        public int Row;
        public int Column; 

        /// <summary>
        /// Called when spawned from the grid board
        /// </summary>
        public virtual void Spawn()
        {

        }
        public virtual void SetRowCol(int row, int col)
        {
            Row = row;
            Column = col;
        }

        public void SetLocalPosition(Vector3 localPosition)
        {
            transform.localPosition = localPosition;
        }
    }
}