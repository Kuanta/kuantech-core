using System;
using System.Collections;
using Kuantech.Core;
using Kuantech.Core.Store;
using Kuantech.Core.UI;
using Kuantech.HyperCasual.UI;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Midcore
{
    public class CoinDrop : MonoBehaviour
    {
        [Header("Target / UI")]
        [SerializeField] private FlyingUIElement FlyingCurrency;

        [Header("Timing")]
        [SerializeField] private float FlyingDelay = 0.5f;     // UI’ya uçmadan önce bekleme
        [SerializeField] private float HopDuration = 0.45f;    // zıplama süresi

        [Header("Hop (World)")]
        [SerializeField] private Vector3 UpVector = default;   // boşsa Vector3.up
        [SerializeField] private float HopHeight = 0.75f;      // tepe yüksekliği (metre)
        [SerializeField] private float HorizontalScatter = 0.6f; // yanal saçılma mesafesi (metre)
        
        [NonSerialized] public CurrencyData CurrencyData;
        private Coroutine _flyRoutine;
        private Coroutine _hopRoutine;
        private CurrencyAsset _targetCurrencyAsset;

        public void Drop(WorldPoint pointToDrop, CurrencyData currencyData)
        {
            CurrencyData = currencyData;
            _targetCurrencyAsset = CurrencyManager.GetCurrencyAssetById(CurrencyData.CurrencyId);
            if (_targetCurrencyAsset == null)
            {
                PoolManager.PoolObject(gameObject);
                return;
            }
            
            // dünyaya koy
            transform.position = pointToDrop.GetTargetPosition();

            // UpVector set edilmemişse world up
            if (UpVector == default || UpVector.sqrMagnitude < 0.0001f)
                UpVector = Vector3.up;
            UpVector.Normalize();

            // önce varsa eski rutinleri kes
            if (_flyRoutine != null) { StopCoroutine(_flyRoutine);  _flyRoutine = null; }
            if (_hopRoutine != null) { StopCoroutine(_hopRoutine);  _hopRoutine = null; }

            // hop başlasın (FlyingDelay’den kısa da olsa sorun değil)
            _hopRoutine = StartCoroutine(HopRoutine(HopDuration));

            // UI’ya uçma süreci
            FlyingCurrency.OnTargetEventReached = OnTargetReached;
            _flyRoutine = StartCoroutine(FlyToUI());
        }

        private IEnumerator FlyToUI()
        {
            yield return new WaitForSeconds(FlyingDelay);

            // hop hâlâ devam ediyorsa durdur (yoksa iki hareket çakışır)
            if (_hopRoutine != null) { StopCoroutine(_hopRoutine); _hopRoutine = null; }

            LevelUI levelUI = UIManager.GetLevelUI();
            RectTransform targetTransform = GetCoinTarget();
            if (!targetTransform)
            {
                OnTargetReached(FlyingCurrency);
                yield break;
            }

            // Şu anki world pozisyonundan UI’ya uç
            levelUI.FlyTowardsUIElementInWorldSpace(FlyingCurrency, transform.position, targetTransform);
        }

        public virtual RectTransform GetCoinTarget()
        {
            // Senin kodun boş liste üzerinde dönüyordu. Gerçek UI’dan çekelim:
            var levelUI = UIManager.GetLevelUI();
            if (!levelUI) return null;

            var indicators = levelUI.GetComponentsInChildren<CurrencyIndicator>(true);
            foreach (var currencyIndicator in indicators)
            {
                if (currencyIndicator && currencyIndicator.CurrencyAsset == _targetCurrencyAsset)
                {
                    return currencyIndicator.GetComponent<RectTransform>();
                }
            }
            return null;
        }

        private void OnTargetReached(FlyingUIElement element)
        {
            // (Opsiyonel) HideWorldVisuals(false);
            CurrencyManager.AddCurrency(CurrencyData.CurrencyId, CurrencyData.CurrencyAmount);
        }

        // ---------------------------
        // World "hop" (parabolik kavis)
        // ---------------------------
        private IEnumerator HopRoutine(float duration)
        {
            // Yanal saçılma doğrultusu: UpVector’a dik rastgele bir eksen
            Vector3 right = Vector3.Cross(UpVector, Vector3.forward);
            if (right.sqrMagnitude < 0.0001f) right = Vector3.Cross(UpVector, Vector3.right);
            right.Normalize();
            Vector3 forwardOnPlane = Vector3.Cross(UpVector, right).normalized;

            // rastgele yön ve mesafe
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 lateralDir = (Mathf.Cos(angle) * right + Mathf.Sin(angle) * forwardOnPlane).normalized;
            Vector3 lateralOffset = lateralDir * UnityEngine.Random.Range(0.25f * HorizontalScatter, HorizontalScatter);

            Vector3 p0 = transform.position;
            Vector3 p1 = p0 + lateralOffset; // yere tekrar “yakın” düşeceği nokta

            float t = 0f;
            duration = Mathf.Max(0.05f, duration);

            // parabola: Lerp(p0,p1,u) + Up * (4*h*u*(1-u))
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                float u = Mathf.Clamp01(t);

                // temel konum (yanal)
                Vector3 basePos = Vector3.Lerp(p0, p1, u);

                // dikey kavis (0..h..0)
                float arc = 4f * u * (1f - u); // 0->1->0
                Vector3 offset = UpVector * (HopHeight * arc);

                transform.position = basePos + offset;
                yield return null;
            }

            // inişte tam hedefe oturt
            transform.position = p1;
            _hopRoutine = null;
        }

        void HideWorldVisuals(bool hide)
        {
            var r = GetComponentInChildren<Renderer>(true);
            if (r) r.enabled = !hide;
        }
    }
}
