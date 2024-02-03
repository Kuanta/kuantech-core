using Kuantech.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.UI
{
    public class FlyingCurrency : FlyingUIElement {
        [SerializeField] private Image CurrencyIcon;

        public void Fly(CurrencyData data, Transform parent, Vector3 startPosition, Vector3 endPosition)
        {
            CurrencyIcon.sprite = data.CurrencyIcon;
            Fly(startPosition, endPosition);
        }        
    }
}