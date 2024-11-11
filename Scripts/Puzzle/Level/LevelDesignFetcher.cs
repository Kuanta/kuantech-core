using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Kuantech.Puzzle
{
    /// <summary>
    /// This component fetches the level designs and returns the level collection
    /// </summary>
    public class LevelDesignFetcher : MonoBehaviour
    {
        [NonSerialized] public LevelDesignDataCollection FetchedCollection;
        public async  UniTask FetchCollection()
        {
            
        }
    }
}