using System.Collections.Generic;
using Kuantech.Data;
using Kuantech.Inventory.Items;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.UI
{
    public class InventoryMenu : UIMenu
    {
        public enum ItemTabTypes
        {
            WeaponsTab,
            HelmetsTab,
            ChestsTab,
            LegsTab,
        }

        [Header("Equipments")]
        
        [Header("Item Frame Prefab")] 
        [SerializeField] private ItemFrame ItemFramePrefab;
        
        [Header("Tabs")]
        [SerializeField] private ScrollRect ScrollRect;
        [SerializeField] private InventoryPanel WeaponsTab;
        [SerializeField] private InventoryPanel HelmetsTab;
        [SerializeField] private InventoryPanel ChestsTab;
        [SerializeField] private InventoryPanel LegsTab;

        [Header("Buttons")] 
        [SerializeField] private Button WeaponsTabButton;
        [SerializeField] private Button HelmetsTabButton;
        [SerializeField] private Button ChestsTabButton;
        [SerializeField] private Button LegsTabButton;
        [SerializeField] private Image SelectedButtonIndicator;
        [SerializeField] private Button CloseButton;
        
        
        private void Start()
        {
            WeaponsTabButton.onClick.AddListener((() =>
            {
                ToggleTab(ItemTabTypes.WeaponsTab);
                ScrollRect.content = WeaponsTab.RectTransform;
                SelectedButtonIndicator.transform.SetParent(WeaponsTabButton.transform,false);
            }));
            HelmetsTabButton.onClick.AddListener((() =>
            {
                ToggleTab(ItemTabTypes.HelmetsTab);
                ScrollRect.content = HelmetsTab.RectTransform;
                SelectedButtonIndicator.transform.SetParent(HelmetsTabButton.transform,false);
            }));
            ChestsTabButton.onClick.AddListener((() =>
            {
                ToggleTab(ItemTabTypes.ChestsTab);
                ScrollRect.content = ChestsTab.RectTransform;
                SelectedButtonIndicator.transform.SetParent(ChestsTabButton.transform,false);
            }));
            LegsTabButton.onClick.AddListener((() =>
            {
                ToggleTab(ItemTabTypes.LegsTab);
                ScrollRect.content = LegsTab.RectTransform;
                SelectedButtonIndicator.transform.SetParent(LegsTabButton.transform,false);
            }));
            
            CloseButton.onClick.AddListener((() =>
            {
                Close();
                UIManager.Instance.MainMenuUI.Show();
            }));
        }

        public override void Show()
        {
            base.Show();
            GameManager.Instance.CameraFollower.SetTargetParameters(GameManager.Instance.ItemsCoordinates);
        }

        public override void Close()
        {
            base.Close();
            UIManager.Instance.MainMenuUI.EquipmentsPanel.Close();
        }

        public void SetupItems(List<Item> items)
        {
            foreach (var item in items)
            {
                AddItem(item);
            }
        }
        
        public void ToggleTab(ItemTabTypes tabType)
        {
            WeaponsTab.gameObject.SetActive(tabType == ItemTabTypes.WeaponsTab);
            HelmetsTab.gameObject.SetActive(tabType == ItemTabTypes.HelmetsTab);
            ChestsTab.gameObject.SetActive(tabType == ItemTabTypes.ChestsTab);
            LegsTab.gameObject.SetActive(tabType == ItemTabTypes.LegsTab);
        }

        public void AddItem(Item item)
        {
            ItemFrame itemFrame = Instantiate(ItemFramePrefab.gameObject).GetComponent<ItemFrame>();
            GetCorrespondingItemPanel(item).AddItem(item, itemFrame);
        }
        
        public void UpdateItem(Item item)
        {
            GetCorrespondingItemPanel(item).UpdateItem(item);
        }

        public void RemoveItem(Item item)
        {
            GetCorrespondingItemPanel(item).RemoveItem(item);
        }

        public void EquipItem(Item item)
        {
            UIManager.Instance.MainMenuUI.EquipmentsPanel.EquipItem(item, item.slotType);
            GetCorrespondingItemPanel(item).EquipItem(item);
            
        }

        public void UnequipItem(Item item)
        {
            UIManager.Instance.MainMenuUI.EquipmentsPanel.UnequipItem(item.slotType);
            GetCorrespondingItemPanel(item).UnequipItem(item);
        }
        
        private InventoryPanel GetCorrespondingItemPanel(Item item)
        {
            if (item.Type == Enums.ItemType.Weapon)
            {
                return WeaponsTab;
            }else if (item.Type == Enums.ItemType.Armor)
            {
                switch (item.slotType)
                {
                    case Enums.EquipmentSlotType.Head:
                        return HelmetsTab;
                    case Enums.EquipmentSlotType.Chest:
                        return ChestsTab;
                    case Enums.EquipmentSlotType.Legs:
                        return LegsTab;
                }
            }

            return null;
        }
    }
}