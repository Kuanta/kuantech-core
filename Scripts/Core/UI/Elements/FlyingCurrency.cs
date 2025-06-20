using Kuantech.Core.Store;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.Core.UI
{
    public class FlyingCurrency : FlyingUIElement {
        [SerializeField] private Image CurrencyIcon;

        public void Fly(CurrencyAsset asset, Transform parent, Vector3 startPosition, Vector3 endPosition)
        {
            CurrencyIcon.sprite = asset.Icon;
            Fly(startPosition, endPosition);
        }        
    }
}