using Kuantech.Core;
using Kuantech.Core.Store;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.UI
{
    public class FlyingCurrency : FlyingUIElement {
        [SerializeField] private Image CurrencyIcon;

        public void Fly(CurrencyAsset asset, Transform parent, Vector3 startPosition, Vector3 endPosition)
        {
            CurrencyIcon.sprite = asset.CurrencyIcon;
            Fly(startPosition, endPosition);
        }        
    }
}