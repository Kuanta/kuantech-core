using Kuantech.ArcadeIdle.UI;
using Kuantech.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.ArcadeIdle
{
    public class ResourceShopEntryElement : MonoBehaviour {
        
        [Header("Visual Elements")]
        [SerializeField] private Image ResourceIcon;
        [SerializeField] private TMP_Text PriceText;
        [SerializeField] private TMP_Text ResourceTitle;
        [SerializeField] private TMP_Text CurrentAmountText;

        [Header("Buttons")]
        [SerializeField] private Button RemoveFromCartButton;
        [SerializeField] private Button AddToCartButton;

        private ResourceShop _resourceShop;
        private ResourceShopPanel _parentPanel;
        public ResourceShopEntry ResourceShopEntry;

        private void Start()
        {
            RemoveFromCartButton.onClick.AddListener(OnRemoveButtonPressed);
            AddToCartButton.onClick.AddListener(OnAddButtonPressed);
        }

        public void Initialize(ResourceShop shop, ResourceShopPanel parentPanel, ResourceShopEntry entry)
        {
            _resourceShop = shop;
            _parentPanel = parentPanel;
            ResourceShopEntry = entry;
            if(ResourceIcon != null && ResourceShopEntry.ResourceData.ResourceIcon != null)
            {
                ResourceIcon.sprite = ResourceShopEntry.ResourceData.ResourceIcon;
            }
            PriceText.text = entry.Price.Stringfy();
            ResourceTitle.text = entry.ResourceData.Name;
        }

        private void OnAddButtonPressed()
        {
            if(_resourceShop == null) return;
            _resourceShop.AddToShoppingCart(ResourceShopEntry.ResourceData);
            UpdateUI();
        }

        private void OnRemoveButtonPressed()
        {
            if (_resourceShop == null) return;
            _resourceShop.RemoveFromShoppingCart(ResourceShopEntry.ResourceData);
            UpdateUI();
        }

        /// <summary>
        /// Updates the button state by reading from the shop cart
        /// </summary>
        public void UpdateUI()
        {
            if(_resourceShop == null) return;
            int currentAmountInCart = _resourceShop.GetCountInCart(ResourceShopEntry.ResourceData);
            CurrentAmountText.text = currentAmountInCart.Stringfy();
            RemoveFromCartButton.gameObject.SetActive(currentAmountInCart > 0);
            CurrentAmountText.gameObject.SetActive(currentAmountInCart > 0);
            _parentPanel.UpdateUI();
        }
    }
}