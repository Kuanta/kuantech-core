using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Puzzle
{
    [CreateAssetMenu(fileName = "Board Tile Collection", menuName = "Kuantech/Puzzle/Board Tile Collection")]
    public class BoardTileCollection : ScriptableObject
    {
        public List<BoardTile> TilePrefabs;
        
        public BoardTile GetTilePrefab(string tileId)
        {
            return TilePrefabs.Find(t => t.GetTileId() == tileId);
        }
    }
}