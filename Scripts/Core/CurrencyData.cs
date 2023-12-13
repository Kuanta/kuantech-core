using UnityEngine;

namespace Kuantech.Core
{
    
    [CreateAssetMenu(fileName = "CurrencyData", menuName = "Kuantech/Currency", order = 0)]
    public class CurrencyData : ScriptableObject {
        public string CurrencyId;
        public Sprite CurrencyIcon;
    }
}