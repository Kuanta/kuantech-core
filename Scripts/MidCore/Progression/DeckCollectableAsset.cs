using Kuantech.Core;
using UnityEngine;

namespace Kuantech.Midcore
{
    [CreateAssetMenu(fileName = "DeckCollectableAsset", menuName = "Kuantech/Collectable/Deck Collectable Asset")]
    public class DeckCollectableAsset : ProgressableDataAsset
    {
        [Header("Actor Blueprint")]
        public ActorBlueprint ActorBlueprint;
    }
}