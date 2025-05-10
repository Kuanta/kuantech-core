using System.Collections.Generic;
using Kuantech.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.ArcadeIdle.UI
{
    public class ResourceShopPanel : ArcadeIdlePanel
    {
        [SerializeField] private TMP_Text TotalPriceText;
        [SerializeField] private TMP_Text CartSizeText;
        [SerializeField] private Transform ElementsParent;
        [SerializeField] private ResourceShopEntryElement EntryElementPrefab;
        [SerializeField] private Button CheckoutButton;

        [SerializeField] private GameObject ShopAvailablePanel;
        [SerializeField] private GameObject ShopUnavailablePanel;
        [SerializeField] private OrderArriveTimeIndicator OrderArriveTimeIndicator;

        private ResourceShop _resourceShop;
        private List<ResourceShopEntryElement> _elements;
        private bool _initialized = false;

        public void Initialize()
        {
            if(_initialized) return;
            _resourceShop = ArcadeIdleManager.GetContext<ArcadeIdleManager>().GetResourceShop();
            _elements = new List<ResourceShopEntryElement>();
            foreach(var resourceEntry in _resourceShop.ShopList)
            {
                ResourceShopEntryElement element = Instantiate(EntryElementPrefab);
                element.transform.SetParent(ElementsParent);
                element.transform.localScale = Vector3.one;
                element.Initialize(_resourceShop, this, resourceEntry);
                _elements.Add(element);
            }
            _initialized = true;
            CheckoutButton.onClick.AddListener(OnCheckoutButtonPressed);
            OrderArriveTimeIndicator.ResourceShop = _resourceShop;
        }

        public override void Open()
        {
            Initialize();
            base.Open();
            foreach (var element in _elements)
            {
                element.UpdateUI();
            }
            UpdateUI();

            bool shopAvailable = _resourceShop.IsAvailable();
            ShopAvailablePanel.SetActive(shopAvailable);
            ShopUnavailablePanel.SetActive(!shopAvailable);
        }

        /// <summary>
        /// Updates the UI. Should be called whenever a resource is added to or removed from the cart
        /// </summary>
        public void UpdateUI()
        {
            CartSizeText.text = $"{_resourceShop.GetTotalItemCountInCart()}/{_resourceShop.GetCartSize()}";
            int cartSum = _resourceShop.GetCartSum();
            int heldCurrency = _resourceShop.GetHeldCurrency();
            TotalPriceText.text = cartSum.Stringfy();
            CheckoutButton.interactable = heldCurrency >= cartSum;
        }

        private void OnCheckoutButtonPressed()
        {
            if(_resourceShop.Checkout())
            {
                Close();
            }
        }
    }
}