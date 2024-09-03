using UnityEngine;

namespace Kuantech.Puzzle
{
    /// <summary>
    /// An unpassable tile, blocks the background tile
    /// </summary>
    public class GridBoardUnpassableTile : GridTile
    {
        public GameObject EditorVisual;
        public override void Spawn()
        {
            base.Spawn();
            if (EditorVisual == null) return;
            EditorVisual.gameObject.SetActive(false); //Visual will never be shown for this type of tiles
        }
    }
}