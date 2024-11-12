using System;
using UnityEngine;

namespace Kuantech.Puzzle
{
    [Serializable]
    public class TileCollectionDictionary : SerializableDictionary<int, GridTile>
    {
        
    }
    [CreateAssetMenu(fileName = "Tile Collection", menuName = "Kuantech/Utils/Tile Collection")]
    public class TileCollection : ScriptableObject
    {
        public TileCollectionDictionary GridTilesByTag;
        public GridTile GetTileBytag(int tag)
        {
            if (GridTilesByTag.ContainsKey(tag))
            {
                return GridTilesByTag[tag];
            }
            return null;
        }
    }
}