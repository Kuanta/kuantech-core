using System.Collections.Generic;
using Kuantech.Inventory.Items;
using UnityEngine;

namespace Kuantech.UI
{
    public class InventoryPanel : MonoBehaviour
    {
        public RectTransform RectTransform;
        private Dictionary<Item, ItemFrame> _itemToFrames = new Dictionary<Item, ItemFrame>();

        private void Awake()
        {
            RectTransform = GetComponent<RectTransform>();
        }

        public void AddItem(Item item, ItemFrame itemFrame)
        {
            if (_itemToFrames.ContainsKey(item))
            {
                Debug.LogWarning($"{item.name} already added to inventory panel ({gameObject.name})");
                return;
            }
            itemFrame.SetupFrame(item);
            _itemToFrames[item] = itemFrame;
            itemFrame.transform.SetParent(transform, false);
        }

        public void UpdateItem(Item item)
        {
            if (!_itemToFrames.ContainsKey(item))
            {
                Debug.LogError($"{gameObject.name} doesn't contain a frame for {item.name}");
                return;
            }

            ItemFrame itemFrame = _itemToFrames[item];
            itemFrame.UpdateCard(item.StateData);
            
        }
        public void RemoveItem(Item item)
        {
            if (!_itemToFrames.ContainsKey(item)) return;
            Destroy(_itemToFrames[item]);
            _itemToFrames.Remove(item);
        }

        public void EquipItem(Item item)
        {
            _itemToFrames[item].ToggleEquippedFrame(true);
        }

        public void UnequipItem(Item item)
        {
            _itemToFrames[item].ToggleEquippedFrame(false);
        }
    }
}