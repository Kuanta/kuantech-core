using System;
using System.Collections.Generic;
using Kuantech.Puzzle;
using UnityEngine;

namespace Kuantech.Inventory
{
    [Serializable]
    public class GridBoardComponentData : ItemComponentData
    {
        public enum ItemShapeType
        {
            Single,
            Horizontal,
            Vertical,
            LongHorizontal,
            LongVertical,
            Square,
        }

        public ItemShapeType ShapeType;
        public Color TileColor = Color.white;

        public override ItemComponent CreateInstance() => new GridBoardComponent(this);

        public List<GridTileCoordinate> GetCoordinates()
        {
            switch (ShapeType)
            {
                default:
                case ItemShapeType.Single:
                    return new List<GridTileCoordinate> { new GridTileCoordinate(0, 0) };
                case ItemShapeType.Horizontal:
                    return new List<GridTileCoordinate> { new GridTileCoordinate(0, 0), new GridTileCoordinate(0, 1) };
                case ItemShapeType.Vertical:
                    return new List<GridTileCoordinate> { new GridTileCoordinate(0, 0), new GridTileCoordinate(1, 0) };
                case ItemShapeType.Square:
                    return new List<GridTileCoordinate> { new GridTileCoordinate(0, 0), new GridTileCoordinate(1, 0), new GridTileCoordinate(0, 1), new GridTileCoordinate(1, 1) };
                case ItemShapeType.LongHorizontal:
                    return new List<GridTileCoordinate> { new GridTileCoordinate(0, 0), new GridTileCoordinate(0, 1), new GridTileCoordinate(0, 2) };
                case ItemShapeType.LongVertical:
                    return new List<GridTileCoordinate> { new GridTileCoordinate(0, 0), new GridTileCoordinate(1, 0), new GridTileCoordinate(2, 0) };
            }
        }
    }
}
