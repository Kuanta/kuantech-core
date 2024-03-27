using System;
using UnityEngine;

namespace Kuantech.Puzzle
{
    public class GridBackground : MonoBehaviour {
        public enum TileTypes
        {
            Center,
            TopEdge,
            LeftEdge,
            RightEdge,
            BottomEdge,
            TopLeftCorner,
            TopRightCorner,
            BottomRightCorner,
            BottomLeftCorner,
        }

        [Serializable]
        public class BackgroundPartDictionary : SerializableDictionary<TileTypes, GameObject>{}

        [SerializeField] private BackgroundPartDictionary _parts;
        public virtual void OnSlotted(int row, int col, GridBoard board)
        {
            bool topValid = board.IsCoordinateValid(row+1, col);
            bool leftValid = board.IsCoordinateValid(row, col-1);
            bool rightValid = board.IsCoordinateValid(row, col+1);
            bool bottomValid = board.IsCoordinateValid(row-1, col);
            TileTypes tileType = TileTypes.Center;

            if(topValid && leftValid && rightValid && bottomValid)
            {
                tileType = TileTypes.Center;
            }
            else if(!topValid && leftValid && rightValid && bottomValid)
            {
                tileType = TileTypes.TopEdge;
            }
            else if(topValid && !leftValid && rightValid && bottomValid)
            {
                tileType = TileTypes.LeftEdge;
            }
            else if(topValid && leftValid && !rightValid && bottomValid)
            {
                tileType = TileTypes.RightEdge;
            }
            else if (topValid && leftValid && rightValid && !bottomValid)
            {
                tileType = TileTypes.BottomEdge;
            }
            else if (!topValid && leftValid && !rightValid && bottomValid)
            {
                tileType = TileTypes.TopRightCorner;
            }
            else if (!topValid && !leftValid && rightValid && bottomValid)
            {
                tileType = TileTypes.TopLeftCorner;
            }
            else if (topValid && !leftValid && rightValid && !bottomValid)
            {
                tileType = TileTypes.BottomLeftCorner;
            }
            else if (topValid && leftValid && !rightValid && !bottomValid)
            {
                tileType = TileTypes.BottomRightCorner;
            }

            foreach(var key in _parts.Keys)
            {
                _parts[key].SetActive(key == tileType);
            }
        }
    }
}