using Kuantech.Core.UI;
using Kuantech.Utils;
using TMPro;
using UnityEngine;

namespace Kuantech.TowerDefense
{
    public class TowerDefenseHUD : UIElement
    {
        [Header("Indicators")]
        [SerializeField] private TMP_Text WaveIndicator;

        [SerializeField] private TMP_Text RemainingEnemyCount;
        
        /// <summary>
        /// Sets the current wave indicator
        /// </summary>
        /// <param name="currentWave"></param>
        /// <param name="totalWaves"></param>
        public void SetWaveIndicator(int currentWave, int totalWaves)
        {
            if (WaveIndicator != null)
            {
                WaveIndicator.text = $"Wave {currentWave} / {totalWaves}";
            }
        }
        
        /// <summary>
        /// Sets the remaining enemy count
        /// </summary>
        /// <param name="count"></param>
        public void SetRemainingEnemyCount(int count)
        {
            if (RemainingEnemyCount != null)
            {
                RemainingEnemyCount.text = count.Stringfy();
            }
        }
    }
}