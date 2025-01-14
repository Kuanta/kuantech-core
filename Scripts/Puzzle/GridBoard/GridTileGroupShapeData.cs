using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Puzzle
{
    [CreateAssetMenu(fileName = "GridTileGroupLayout", menuName = "Kuantech/Puzzle/GridTileGroupLayout")]
    public class GridTileGroupShapeData : ScriptableObject
    {
        public List<GridTileCoordinate> LocalCoordinates;
    }
}