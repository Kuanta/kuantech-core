using DG.Tweening;
using Kuantech.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.UI
{
    public class FlyingUIElement : MonoBehaviour
    {
        [SerializeField] private float FlyDuration;
        [SerializeField] private bool UsePool = false;

        public virtual void Fly(Vector3 startPosition, Vector3 endPosition, object data=null, UnityAction OnTargetReachedHandler = null)
        {
            transform.position = startPosition;
            transform.DOMove(endPosition, FlyDuration).SetEase(Ease.Linear).OnComplete(() =>
            {
                OnTargetReachedHandler?.Invoke();
                if (UsePool)
                {
                    GameManager.Instance.Pool.PoolObject(gameObject);
                }else{
                    Destroy(gameObject);
                }
            });
        }
    }
}