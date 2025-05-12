using UnityEngine;

namespace Kuantech.Core.Store
{
    
    [CreateAssetMenu(fileName = "CurrencyData", menuName = "Kuantech/Currency", order = 0)]
    public class CurrencyAsset : ScriptableObject {
        public string CurrencyId;
        public string CurrencyName;
        public Sprite CurrencyIcon;
    }
}