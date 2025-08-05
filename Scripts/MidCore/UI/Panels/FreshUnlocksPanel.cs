using System.Collections.Generic;
using Kuantech.Core.UI;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Midcore.UI
{
    public class FreshUnlocksPanel : UIMenu
    {
        [SerializeField] private Transform NewUnlocksContainer;
        [SerializeField] private FreshUnlockIndicator FreshUnlockIndicatorPrefab;

        public void ShowFreshUnlocks(HashSet<CollectableAsset> freshCollectables)
        {
            if (freshCollectables.IsNullOrEmpty())
            {
                Close();
                return;
            }
            
            NewUnlocksContainer.DestroyAllChildren();
            foreach (var collectable in freshCollectables)
            {
                FreshUnlockIndicator unlockIndicator = Instantiate(FreshUnlockIndicatorPrefab);
                unlockIndicator.SetAsset(collectable);
                unlockIndicator.transform.SetParent(NewUnlocksContainer);
                unlockIndicator.transform.localScale = Vector3.one;
            }
        }

    }
}