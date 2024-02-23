using System.Collections.Generic;
using UnityEngine;
namespace Kuantech.Puzzle
{
    
    [CreateAssetMenu(fileName = "GridTileLibrary", menuName = "Kuantech/Puzzle/GridTileLibrary")]
    public class GridTileLibrary : ScriptableObject {
        public List<GameObject> Tiles;
    }
}