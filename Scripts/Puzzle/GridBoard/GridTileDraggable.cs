using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Puzzle
{
    public class GridTileDraggable : Draggable
    {
        public struct NeighTileData
        {
            public int Row;
            public int Col;
            public GridTile NeighTile;
        }
        public GridTile AnchorGridTile;
        public List<NeighTileData> NeighbourTiles;
        private void Start()
        {
            if(AnchorGridTile == null) AnchorGridTile = GetComponent<GridTile>();
        }

        /// <summary>
        /// Adds a neighbouring tile
        /// </summary>
        /// <param name="neighTile"></param>
        /// <param name="localRow"></param>
        /// <param name="localCol"></param>
        public void AddNeighbourTile(GridTile neighTile, int localRow, int localCol)
        {
            if(NeighbourTiles == null)
            {
                NeighbourTiles = new List<NeighTileData>();
            }
            //Check if we have duplicate
            foreach(var data in NeighbourTiles)
            {
                if(data.Row == localRow && data.Col == localCol)
                {
                    Debug.LogWarning("Duplicate row-col position in neighbouring tiles");
                    break;
                }
            }
            NeighbourTiles.Add(new NeighTileData()
            {
                Row = localRow,
                Col = localCol,
                NeighTile = neighTile,
            });
        }
    }
}