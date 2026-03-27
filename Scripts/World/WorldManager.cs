using Cysharp.Threading.Tasks;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.World
{
    public class WorldManager : SubManager
    {
        [Header("World!")]
        public World World;

        public override async UniTask Initialize(GameManager gameManager)
        {
            if (World != null)
            {
                World.LoadLevel();
            }
        }
    }
}
