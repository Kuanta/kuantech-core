using System;
using Kuantech.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Core.HyperCasual
{
    [Serializable]
    public class CurrencyDictionary : SerializableDictionary<Currencies, CurrencyIndicator>{}
    
    /// <summary>
    /// Header panel is the common panel that will be used on all screens
    /// </summary>
    public class HeaderPanel : UIMenu
    {
        [SerializeField] private CurrencyDictionary CurrencyDictionary = new CurrencyDictionary();

        [Header("Buttons")] 
        public Button SettingsButton;

        [Header("Texts")] 
        public TMP_Text CurrentLevelText;
        
        [Header("Panels")] 
        public SettingsMenu SettingsMenu;

        private void Start()
        {
            if (SettingsButton != null)
            {
                SettingsButton.onClick.AddListener((() =>
                {
                    SettingsMenu.Show();
                }));
            }
        }
        public void SetCurrencyAmount(Currencies currencyId, int amount)
        {
            if (CurrencyDictionary.ContainsKey(currencyId))
            {
                CurrencyDictionary[currencyId].SetAmount(amount);
            }
        }

        public void SetCurrentLevel(int levelIndex)
        {
            CurrentLevelText.text = $"Stage {(levelIndex+1).ToString()}";
        }
    }
}