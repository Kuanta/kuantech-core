using Kuantech.Core;
using Kuantech.Core.HyperCasual;
using UnityEngine;

namespace Kuantech.Puzzle
{
    [CreateAssetMenu(fileName = "PuzzleBoosterData", menuName = "Kuantech/Puzzle/Booster")]
    public class PuzzleBooster : ScriptableObject
    {
        [Header("Description")] 
        public string Title;
        public string Description;
        public Sprite Icon;

        public bool ShowUIOnActivation;
        
        [Header("Requirements")]
        [Header("Price")] 
        public CurrencyData PriceCurrencyType;
        public int Price;

        [Header("Level")] 
        public int LevelRequirement;

        protected PuzzleLevel CurrentLevel;
        public virtual void ActivateBooster(PuzzleLevel currentLevel)
        {
            CurrentLevel = currentLevel;
        }

        public virtual void CancelBooster()
        {
            if (CurrentLevel != null)
            {
                CurrentLevel.CancelCurrentBooster();
            }
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
            if (PriceCurrencyType == null) return true;
            return StoreManager.RemoveCurrency(PriceCurrencyType, Price);
        }
        
        /// <summary>
        /// Checks if there are enough money for the currency
        /// </summary>
        /// <returns></returns>
        public bool CanBeBought()
        {
            if (PriceCurrencyType == null) return true;
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
        
                
        #region Common Functionalities
        public virtual void OnTileTapped(GridTile tile)
        {
            
        }
        #endregion
    }
}