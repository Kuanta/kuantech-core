using DG.Tweening;
using Kuantech.Core;
using Kuantech.Core.FX;
using Kuantech.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.UI
{
    public class FlyingUIElement : MonoBehaviour
    {
        [SerializeField] private float FlyDuration;
        [SerializeField] private bool UsePool = false;
        [SerializeField] private Ease Ease = Ease.Linear;
        [KTTag("AudioTag")]
        [SerializeField] private int AudioTag;

        public virtual void Fly(Vector3 startPosition, Vector3 endPosition, object data=null, UnityAction OnTargetReachedHandler = null)
        {
            transform.localPosition = startPosition;
            transform.DOMove(endPosition, FlyDuration).SetEase(Ease).OnComplete(() =>
            {
                OnTargetReachedHandler?.Invoke();
                EffectsLibrary.PlayAudio(AudioTag);
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