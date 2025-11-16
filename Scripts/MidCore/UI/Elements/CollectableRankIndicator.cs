using Kuantech.Core.UI;
using Kuantech.Rpg.UI;
using UnityEngine;

namespace Kuantech.Midcore.UI
{
    public class CollectableRankIndicator : UIElement
    {
        [SerializeField] private LevelableFloatIndicator LevelIndicator;
        
        /// <summary>
        /// Updates the rank
        /// </summary>
        /// <param name="collectableAsset"></param>
        public void SetCollectableRank(CollectableAsset collectableAsset)
        {
            LevelIndicator.UpdateValue(collectableAsset.GetCollectableRank());
        }
    }
}