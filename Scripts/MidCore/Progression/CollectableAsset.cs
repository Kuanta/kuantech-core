using Kuantech.Core;
using UnityEngine;

namespace Kuantech.Midcore
{
    [CreateAssetMenu(fileName = "DeckCollectableAsset", menuName = "Kuantech/Collectable/Deck Collectable Asset")]
    public class CollectableAsset : ProgressableDataAsset
    {
        [Header("Actor Blueprint")]
        public ActorBlueprint ActorBlueprint;

        [Header("Unlock condition")] 
        public int RequiredLevel;
    }
}