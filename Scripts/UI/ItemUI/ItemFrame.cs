using System.Collections.Generic;
using System.Globalization;
using Kuantech.Core;
using Kuantech.Inventory.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.UI
{
    public class ItemFrame : MonoBehaviour
    {


        [Header("Click Button")] 
        [SerializeField] private Button FrameButton; //This button is used for equip/unequip
        [SerializeField] private GameObject EquipedFrame;
        
        [Header("Text Fields")] 
        public TMP_Text ItemName;
        public TMP_Text LevelText;
        public Image ItemBaseStatImage;
        public RectTransform StatMultipliersParent;
        public TMP_Text BaseValueText; //Text for damage (for weapons) or armor (for armors?)

        public Image Background;

        public Item AssignedItem;
        
        [SerializeField] private GameObject StatMultiplierLevelPrefab;
        private Dictionary<StatTypes, StatMultiplierLabel> _statMultipliers;

        private void Awake()
        {
            FrameButton.onClick.AddListener(() =>
            {
                if (AssignedItem == null) return;
                if (!AssignedItem.StateData.Equipped)
                {
                    GameManager.Instance.EquipItem(AssignedItem);
                }
                else
                {
                    GameManager.Instance.UnequipItem(AssignedItem);
                }
            });
        }

        /// <summary>
        /// Sets the frame from item stateData
        /// </summary>
        /// <param name="itemStateData">State data of the item</param>
        /// <param name="itemName">Name of the item</param>
        public void SetupFrame(Item item)
        {
            AssignedItem = item;
            ItemStateData itemStateData = item.StateData;
            ItemName.text = item.name;
            EquipedFrame.SetActive(itemStateData.Equipped);
            SetLevel(itemStateData.ItemLevel);
            SetRarity(itemStateData.ItemRarity);
            if (item is Weapon weapon)
            {
                SetBaseStat(weapon.BaseStat); //Todo: Add base stat field to items
                BaseValueText.text = weapon.GetDamage(0).ToString(CultureInfo.InvariantCulture);
            }else if (item is Armor)
            {
                SetBaseStat(StatTypes.Armor);
                BaseValueText.text = ((Armor) item).armorRating.ToString(CultureInfo.InvariantCulture);
            }
            if (itemStateData.StatModifiers == null) return;
            foreach (var stat in itemStateData.StatModifiers.Keys)
            {
                AddStatMultiplier(stat, itemStateData.StatModifiers[stat].GetValue());
            }

        }

        public void SetBaseStat(StatTypes statType)
        {
            ItemBaseStatImage.sprite = UIManager.Instance.IconLibrary.GetStatIcon(statType);
            Color color = UIManager.Instance.ColorPalette.StatColors[statType];
            color.a = 0.25f;
            Background.color = color;
        }
        
        public void SetRarity(ItemRarities rarityLevel)
        {
            switch (rarityLevel)
            {
                case ItemRarities.Common:
                    ItemName.text = AssignedItem.name;
                    ItemName.color = UIManager.Instance.ColorPalette.CommonColor;
                    break;
                case ItemRarities.Uncommon:
                    ItemName.text = "Uncommon "+AssignedItem.name;
                    ItemName.color = UIManager.Instance.ColorPalette.UncommonColor;
                    break;
                case ItemRarities.Rare:
                    ItemName.text = "Rare "+AssignedItem.name;
                    ItemName.color = UIManager.Instance.ColorPalette.RareColor;
                    break;
                case ItemRarities.Epic:
                    ItemName.text = "Epic "+AssignedItem.name;
                    ItemName.color = UIManager.Instance.ColorPalette.EpicColor;
                    break;
                case ItemRarities.Legendary:
                    ItemName.text = "Legendary "+AssignedItem.name;
                    ItemName.color = UIManager.Instance.ColorPalette.LegendaryColor;
                    break;
            }    
        }
        
        public void SetLevel(int level)
        {
            LevelText.text = $"{level:D}";
        }

        public void AddStatMultiplier(StatTypes statType, float multiplier)
        {
            if (_statMultipliers == null) _statMultipliers = new Dictionary<StatTypes, StatMultiplierLabel>();
            if (_statMultipliers.ContainsKey(statType))
            {
                _statMultipliers[statType].SetMultiplier(multiplier);
                return;
            }

            GameObject label = Instantiate(StatMultiplierLevelPrefab);
            if (label.TryGetComponent(out StatMultiplierLabel multiplierLabel))
            {
                label.transform.SetParent(StatMultipliersParent);
                multiplierLabel.SetMultiplier(statType, multiplier);
                _statMultipliers[statType] = multiplierLabel;
            }
            else
            {
                Destroy(label);
            }
        }
        
        /// <summary>
        /// Updates the item card
        /// </summary>
        /// <param name="itemStateData"></param>
        public void UpdateCard(ItemStateData itemStateData)
        {
            EquipedFrame.SetActive(itemStateData.Equipped);
            
            //Todo: Optimize here
            SetLevel(itemStateData.ItemLevel);
            SetRarity(itemStateData.ItemRarity);
            SetBaseStat(StatTypes.Strength); //Todo: Add base stat field to items

            if (itemStateData.StatModifiers == null) return;
            foreach (var statType in itemStateData.StatModifiers.Keys)
            {
                if (_statMultipliers.ContainsKey(statType))
                {
                    _statMultipliers[statType].SetMultiplier(itemStateData.StatModifiers[statType].GetValue());
                }
                else
                {
                    AddStatMultiplier(statType, itemStateData.StatModifiers[statType].GetValue());
                }
            }
            
            //todo: Check for stat multiplier removal
        }

        public void ToggleEquippedFrame(bool toggle)
        {
            EquipedFrame.SetActive(toggle);
        }
    }
}