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

        public void SetLevelIndex(int levelIndex)
        {
            LevelIndexText.text = $"{LevelPrefix} {(levelIndex).ToString()}";
        }
    }
}