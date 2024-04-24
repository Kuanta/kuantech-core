using Kuantech.Core;
using Kuantech.Core.HyperCasual;
using UnityEngine;

namespace Kuantech.Puzzle
{
    [CreateAssetMenu(fileName = "PuzzleBooster", menuName = "Kuantech/Puzzle/Booster")]
    public class PuzzleBooster : ScriptableObject
    {
        [Header("Description")] 
        public string Title;
        public string Description;

        [Header("Price")] 
        public CurrencyData PriceCurrencyType;
        public int Price;

        public virtual bool OnSetBooster(PuzzleLevel currentLevel)
        {
            return CanBeBought();
        }

        public virtual void CancelBooster()
        {
            
        }
        
        /// <summary>
        /// Completes the booster by spending the money
        /// </summary>
        public virtual bool CompleteBooster()
        {
            return BuyBooster();
        }
        
        /// <summary>
        /// Buys the booster
        /// </summary>
        public bool BuyBooster()
        {
            return StoreManager.RemoveCurrency(PriceCurrencyType, Price);
        }
        
        /// <summary>
        /// Checks if there are enough money for the currency
        /// </summary>
        /// <returns></returns>
        public bool CanBeBought()
        {
            return StoreManager.HasCurrency(PriceCurrencyType, Price);
        }

        public string GetBoosterTitle()
        {
            return Title;
        }
        
        public string GetBoosterDescription()
        {
            return Description;
        }
    }
}