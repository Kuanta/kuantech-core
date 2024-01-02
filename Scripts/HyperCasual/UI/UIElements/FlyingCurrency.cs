using DG.Tweening;
using Kuantech.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Kuantech.HyperCasual.UI
{
    public class FlyingCurrency : MonoBehaviour {
        [SerializeField] private Image CurrencyIcon;
        [SerializeField] private float Speed;

        public void Fly(CurrencyData data, Transform parent, Vector3 startPosition, Vector3 endPosition)
        {
            CurrencyIcon.sprite = data.CurrencyIcon;
            transform.SetParent(parent);
            transform.position = startPosition;
            transform.DOMove(endPosition, Speed).SetEase(Ease.Linear).OnComplete(()=>{
                GameManager.Instance.Pool.PoolObject(gameObject);
            });
        }        
    }
}