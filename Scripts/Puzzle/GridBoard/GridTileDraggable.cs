using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Puzzle
{
    [RequireComponent(typeof(GridTile))]
    public class GridTileDraggable : Draggable
    {
        public GridTile GridTile;

        private void Start()
        {
            if(GridTile == null) GridTile = GetComponent<GridTile>();
        }
    }
}