using Kuantech.Core.UI;
using Kuantech.Rpg;
using UnityEngine;

namespace Kuantech.TowerDefense
{
    public class TowerDefenseLevelUI : LevelUI
    {
        [Header("UI Elements")]
        [SerializeField] private Healthbar Healthbar;

        #region Health

        public void SetHealthText(float health, float maxHealth)
        {
            Healthbar.SetHealth(health, maxHealth);
        }

        #endregion
    }
}