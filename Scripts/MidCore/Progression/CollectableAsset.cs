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

        [Header("Deck Index")] 
        public int DeckIndex = 0;
        
        [Tooltip("If set to true, it will be unlocked automatically when the player reaches the required level")]
        public bool UnlockAutomacically = true;
        
        /// <summary>
        /// Returns the collectable rank. If some custom logic is required other than the progressible rank,
        /// it can be implemented here
        /// </summary>
        /// <returns></returns>
        public virtual int GetCollectableRank()
        {
            return ProgressionManager.GetCurrentRank(this);
        }

        public override Sprite GetIcon()
        {
            Sprite sprite = base.GetIcon();
            if (sprite != null) return sprite;
            if (sprite == null && ActorBlueprint != null && ActorBlueprint.GetIcon() != null)
            {
                return ActorBlueprint.GetIcon();
            }

            return null;
        }
    }
}