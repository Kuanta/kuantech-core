using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Puzzle
{
    [Serializable]
    public class BoardSubZone
    {
        [Tooltip("Coorinate in the form of (col, row)")]
        public List<Vector2Int> Coordinates;
        public int BoardSubZoneColorId;

        public bool IsCoordinateInZone(int row, int col)
        {
            return Coordinates.Contains(new Vector2Int(col, row));
        }
    }
}