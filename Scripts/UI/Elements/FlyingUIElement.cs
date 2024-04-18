using DG.Tweening;
using Kuantech.Core;
using Kuantech.Core.FX;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.UI
{
    public class FlyingUIElement : MonoBehaviour
    {
        [SerializeField] private float FlySpeed = 100.0f; // Speed at which the object will fly
        [SerializeField] private bool UsePool = false;
        [SerializeField] private Ease Ease = Ease.Linear;
        public Vector3 StartNoise;
        public Vector3 EndNoise;
        //Events
        public UnityAction<FlyingUIElement> OnTargetEventReached;

        [Header("Effect")]
        [SerializeField] private EffectPlayer ScoreReachedEffect;

        public virtual void Fly(Vector3 startPosition, Vector3 endPosition, object data=null, UnityAction OnTargetReachedHandler = null)
        {
            startPosition += new Vector3(Random.Range(-StartNoise.x, StartNoise.x), Random.Range(-StartNoise.y, StartNoise.y), Random.Range(-StartNoise.z, StartNoise.z));
            endPosition += new Vector3(Random.Range(-EndNoise.x, EndNoise.x), Random.Range(-EndNoise.y, EndNoise.y), Random.Range(-EndNoise.z, EndNoise.z));
            Vector3 diff = endPosition - startPosition;
            diff.z = 0;
            // Calculate the distance between start and end positions
            float distance = diff.magnitude;

            // Calculate the duration based on the distance and fly speed
            float duration = distance/FlySpeed;
            transform.localPosition = startPosition + new Vector3(Random.Range(-StartNoise.x, StartNoise.x), Random.Range(-StartNoise.y, StartNoise.y), Random.Range(-StartNoise.z, StartNoise.z));
            transform.DOLocalMove(endPosition, duration).SetEase(Ease).OnComplete(() =>
            {
                OnTargetReached(OnTargetReachedHandler);
                ScoreReachedEffect?.PlayEffectAtPosition(transform.position, Quaternion.identity);
                if (UsePool)
                {
                    GameManager.Instance.Pool.PoolObject(gameObject);
                }else{
                    Destroy(gameObject);
                }
            });
        }

        protected virtual void OnTargetReached(UnityAction OnTargetReachedHandler)
        {
            OnTargetReachedHandler?.Invoke();
            OnTargetEventReached?.Invoke(this);

        }
    }
}