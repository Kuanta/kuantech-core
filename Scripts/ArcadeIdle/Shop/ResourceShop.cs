using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.HyperCasual;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    [Serializable]
    public struct ResourceShopEntry
    {
        public ResourceData ResourceData;
        public int Price;
    }

    public class ResourceShop : MonoBehaviour {
        public CurrencyData UsedCurrency;
        public List<ResourceShopEntry> ShopList;
        private Dictionary<ResourceData, ResourceShopEntry> _shopListMap = new Dictionary<ResourceData, ResourceShopEntry>(); //For quick lookups
        public Dictionary<ResourceData, int> ShoppingCart = new Dictionary<ResourceData, int>(); 
        public LeveledValueInt CartSize; //Max amount of items cart can have
        public UpgradeData CartSizeUpgrade;
        public float BaseOrderTime;
        public LeveledValueFloat OrderTimeMultiplier;
        public UpgradeData OrderTimeUpgrade;

        private bool _orderIncoming = false;
        private float _orderTime;
        private Dictionary<ResourceData, int> _orderedResources;

        //Events
        public EventHandler OnCheckoutEvent;
        public EventHandler<Dictionary<ResourceData, int>> OnOrderArrivedEvent;

        private void Start()
        {
            _shopListMap = new Dictionary<ResourceData, ResourceShopEntry>();
            foreach(var entry in ShopList)
            {
                _shopListMap[entry.ResourceData] = entry;
            }
        }

        private void Update()
        {
            if(!_orderIncoming) return;
            if(Time.time - _orderTime >= GetOrderDeliveryTime())
            {
                //Order arrived
                OnOrderArrived();
            }
        }

        #region Shopping Cart
        /// <summary>
        /// Adds a resource to chart
        /// </summary>
        /// <param name="resource"></param>
        public bool AddToShoppingCart(ResourceData resource)
        {
            if(!_shopListMap.ContainsKey(resource) || GetTotalItemCountInCart() >= GetCartSize()) return false; //Don't add resources that is not in the shopping list
            if(ShoppingCart == null) ShoppingCart = new Dictionary<ResourceData, int>();
            if(!ShoppingCart.ContainsKey(resource))
            {
                ShoppingCart[resource] = 0;
            }

            ShoppingCart[resource] += 1;
            return true;
        }

        /// <summary>
        /// Removes a resource from chart
        /// </summary>
        /// <param name="resource"></param>
        public bool RemoveFromShoppingCart(ResourceData resource)
        {
            if (!_shopListMap.ContainsKey(resource)) return false; //Don't remove resources that is not in the shopping list
            if (ShoppingCart == null || 
            !ShoppingCart.ContainsKey(resource) || 
            ShoppingCart[resource] <= 0) return false;
            ShoppingCart[resource] -= 1;
            return true;
        }

        /// <summary>
        /// Returns 
        /// </summary>
        /// <param name="resourceData"></param>
        /// <returns></returns>
        public int GetCountInCart(ResourceData resourceData)
        {
            if(ShoppingCart == null || !ShoppingCart.ContainsKey(resourceData)) return 0;
            return ShoppingCart[resourceData];
        }
        
        public int GetTotalItemCountInCart()
        {
            int sum = 0;

            foreach (var pair in ShoppingCart)
            {
                sum += pair.Value;
            }

            return sum;
        }

        /// <summary>
        /// Gets the total price of the sum
        /// </summary>
        /// <returns></returns>
        public int GetCartSum()
        {
            int sum = 0;

            foreach (var pair in ShoppingCart)
            {
                sum += _shopListMap[pair.Key].Price * pair.Value;
            }

            return sum;
        }

        /// <summary>
        /// Gets the total number of items that can be bought1
        /// </summary>
        /// <returns></returns>
        public int GetCartSize()
        {
            if(CartSizeUpgrade == null) return CartSize.GetValue(0);
            UpgradeManager um = UpgradeManager.GetContext<UpgradeManager>();
            if(um == null) return CartSize.GetValue(0);
            int upgradeId = UpgradeManager.GetCurrentUpgradeLevel(CartSizeUpgrade.UpgradeId);
            return CartSize.GetValue(upgradeId);
        }
        #endregion

        /// <summary>
        /// Tries to purchase all items in cart. Returns the result as boolean.
        /// </summary>
        public bool Checkout()
        {
            int totalPrice = GetCartSum();

            //Get currency
            int heldCurrency = GetHeldCurrency();
            if(heldCurrency < totalPrice) return false;
            //todo:(currency) : Fix here
            //CurrencyModel cm = GameStateManager.GetModuleStatic<CurrencyModel>();
            //cm.RemoveCurrency(UsedCurrency.CurrencyId, totalPrice);
            _orderedResources = new Dictionary<ResourceData, int>();
            
            foreach(var pair in ShoppingCart)
            {
                _orderedResources[pair.Key] = pair.Value;
            }

            Reset();
            _orderIncoming = true;
            _orderTime = Time.time;
            OnCheckoutEvent?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Gets the held currency
        /// </summary>
        /// <returns></returns>
        public int GetHeldCurrency()
        {
            //todo:(currency) : Fix here
            //return cm.GetCurrencyAmount(UsedCurrency.CurrencyId);
            return 0;
        }

        public bool IsOrderIncoming()
        {
            return _orderIncoming;
        }
        public bool IsAvailable()
        {
            if(_orderIncoming) return false;
            return true;
        }

        public float GetOrderDeliveryTime()
        {
            return BaseOrderTime; //* 1/(OrderTimeMultiplier.GetValue(UpgradeManager.GetCurrentUpgradeLevel(OrderTimeUpgrade.UpgradeId)));
        }

        /// <summary>
        /// Returns the remaining seconds for the order to arrive.
        /// </summary>
        /// <returns></returns>
        public float GetRemainingTime()
        {
            return GetOrderDeliveryTime() - (Time.time - _orderTime);
        }
        private void OnOrderArrived()
        {
            OnOrderArrivedEvent?.Invoke(this, _orderedResources);
            _orderIncoming = false;
        }

        /// <summary>
        /// Resets the chart
        /// </summary>
        public void Reset()
        {
            ShoppingCart.Clear();
        }
    }
}