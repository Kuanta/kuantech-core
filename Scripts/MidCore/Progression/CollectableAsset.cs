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
        
        [Tooltip("If set to true, it will be unlocked automatically when the player reaches the required level")]
        public bool UnlockAutomacically = true;
    }
}