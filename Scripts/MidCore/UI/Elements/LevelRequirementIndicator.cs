using TMPro;
using UnityEngine;

namespace Kuantech.Midcore.UI
{
    /// <summary>
    /// An indicator for level requirement of progressable assets
    /// </summary>
    public class LevelRequirementIndicator : MonoBehaviour
    {
        [SerializeField] private TMP_Text RequirementText;
        [SerializeField] private string Prefix = "Level ";
        [SerializeField] private int LevelOffset = 1;

        public void SetProgressable(ProgressableDataAsset asset, int rank)
        {
            ProgressableDependencyEntry unlockConditions = ProgressionManager.GetUnlockConditions(asset, rank);
            if (unlockConditions != null && unlockConditions.RequiredPlayerRank > 0)
            {
                RequirementText.gameObject.SetActive(true);
                RequirementText.text = $"{Prefix}{unlockConditions.RequiredPlayerRank + LevelOffset}";
            }
            else
            {
                RequirementText.gameObject.SetActive(false);
            }
        }
    }
}