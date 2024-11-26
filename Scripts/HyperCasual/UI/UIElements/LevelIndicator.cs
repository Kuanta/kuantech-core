using Kuantech.Puzzle.UI;
using TMPro;
using UnityEngine;

namespace Kuantech.Core.HyperCasual.UI
{
    /// <summary>
    /// An indicator that shows the current level index. Not useful for games that doesn't have a level system
    /// </summary>
    public class LevelIndicator : MonoBehaviour
    {
        [SerializeField] private TMP_Text LevelIndexText;
        [SerializeField] private string LevelPrefix = "Level";
        [SerializeField] private GameObject HardLevelIndicator;
        [SerializeField] private GameObject BonusLevelIndicator;
        [SerializeField] private StageIndicator StageIndicator;
        public void SetLevelIndex(int levelIndex)
        {
            LevelIndexText.text = $"{LevelPrefix} {(levelIndex).ToString()}";
        }

        public void SetHardLevel(bool isHardLevel)
        {
            if (HardLevelIndicator == null) return;
            HardLevelIndicator.SetActive(isHardLevel);
        }

        public void SetBonusLevel(bool isBonusLevel)
        {
            if (BonusLevelIndicator == null) return;
            BonusLevelIndicator.SetActive(isBonusLevel);
        }

        public void SetStageCount(int stageCount)
        {
            if (StageIndicator == null) return;
            // if (stageCount <= 1)
            // {
            //     StageIndicator.gameObject.SetActive(false);
            //     return;
            // }
            StageIndicator.gameObject.SetActive(true);
            StageIndicator.SetupForLevel(stageCount);
        }

        public void SetCurrentStage(int currentStage)
        {
            if (StageIndicator == null) return;
            StageIndicator.SetCurrentStage(currentStage);
        }
    }
}