using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Kuantech.Combat.Kuantech.Core;
using UnityEngine;

namespace Kuantech.Core
{
    public class CombatManager : SubManager
    {
        [Header("Damage Texts")] [SerializeField]
        private FloatingDamageText DamageTextPrefab;

        [SerializeField]
        private FloatingDamageText HealTextPrefab;
        
        /// <summary>
        /// Struct to be used by other classes
        /// </summary>
        [Serializable]
        public struct HitStopEntry
        {
            public float Duration;
            public float TimeScale;
            public float Priority;
            public CombatManager.HitStopPushType PushType;
        }
        
        //Hit stop
        private class HitStop
        {
            public float TargetScale;
            public float EndUnscaledTime;
            public float Priority = 0;
        }

        public enum HitStopPushType
        {
            Default,     // Add to queue
            AddIfEmpty,  // If empty, add
            Replace,      // Clear all, add this
            ReplaceIfPrior
        }
        
        private readonly List<HitStop> _stops = new List<HitStop>();

        private float _baseFixedDeltaTime;
        private float _appliedScale = 1f;
        
        #region Damage Texts
        public static void ShowDamageText(Vector3 position, DamageInfo damageInfo, bool friendly)
        {
            CombatManager ctx = GetContext<CombatManager>();
            if (ctx == null || ctx.DamageTextPrefab == null) return;
            FloatingDamageText damageText =
                PoolManager.GetObjectFromPool(ctx.DamageTextPrefab.gameObject).GetComponent<FloatingDamageText>();
           if(damageText == null) return;
           damageText.transform.position = position;
           damageText.Show(damageInfo, friendly);
        }
        
        public static void ShowHealText(Vector3 position, float amount, bool friendly, bool isCritical)
        {
            CombatManager ctx = GetContext<CombatManager>();
            if (ctx == null) return;
            FloatingDamageText damageText =
                PoolManager.GetObjectFromPool(ctx.HealTextPrefab.gameObject).GetComponent<FloatingDamageText>();
            if (damageText == null) return;
            damageText.transform.position = position;
            damageText.Show(amount, friendly, isCritical);
        }
        #endregion
        
        public async override UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);
            _baseFixedDeltaTime = Time.fixedDeltaTime;
        }

        private void Update()
        {
            if (!Initialized) return;
            UpdateHitStops();

        }

        #region HitStop

        private void UpdateHitStops()
        {
            ClearExpiredStops();
            float newScale = 1f;
            for (int i = 0; i < _stops.Count; i++)
                newScale = Mathf.Min(newScale, _stops[i].TargetScale);

            if (!Mathf.Approximately(newScale, _appliedScale))
                ApplyTimeScale(newScale);
        }
        
        private bool HasActiveHitStop()
        {
            float now = Time.unscaledTime;
            for (int i = 0; i < _stops.Count; i++)
                if (_stops[i].EndUnscaledTime > now)
                    return true;
            return false;
        }

        private float GetCurrentActivePriority()
        {
            float now = Time.unscaledTime;
            float maxPrior = float.NegativeInfinity;
            bool any = false;

            for (int i = 0; i < _stops.Count; i++)
            {
                var s = _stops[i];
                if (s.EndUnscaledTime > now)
                {
                    any = true;
                    if (s.Priority > maxPrior) maxPrior = s.Priority;
                }
            }
            return any ? maxPrior : float.NegativeInfinity;
        }
        
        private void ClearExpiredStops()
        {
            float now = Time.unscaledTime;
            for (int i = _stops.Count - 1; i >= 0; i--)
                if (now >= _stops[i].EndUnscaledTime)
                    _stops.RemoveAt(i);
        }
        private void ClearHitStops()
        {
            ApplyTimeScale(1f);
            _stops.Clear();
        }
        
        private void ApplyTimeScale(float scale)
        {
            _appliedScale = Mathf.Clamp(scale, 0f, 1f);
            Time.timeScale = _appliedScale;
            Time.fixedDeltaTime = _baseFixedDeltaTime * Mathf.Max(_appliedScale, 0.0001f);
        }

        public void PushHitStop(HitStopEntry entry)
        {
            PushHitStop(entry.TimeScale, entry.Duration, entry.PushType, entry.Priority);
        }
        
        public void PushHitStop(float scale, float durationSeconds, HitStopPushType pushType = HitStopPushType.Default, float priority=0f)
        {
            scale = Mathf.Clamp(scale, 0f, 1f);
            float now = Time.unscaledTime;
            float end = now + Mathf.Max(0f, durationSeconds);

            // Önce süresi bitenleri temizle
            ClearExpiredStops();

            switch (pushType)
            {
                case HitStopPushType.AddIfEmpty:
                    if (_stops.Count > 0 || HasActiveHitStop())
                        return; 
                    break;

                case HitStopPushType.Replace:
                    _stops.Clear(); // Clear all
                    break;

                case HitStopPushType.ReplaceIfPrior:
                {
                    float currentPrior = GetCurrentActivePriority(); // -∞ ise aktif yok
                    // Yalnızca "daha yüksek" öncelikte ise replace et (eşitse dokunma)
                    if (priority > currentPrior)
                    {
                        _stops.Clear();
                    }
                    else
                    {
                        return; // daha öncelikli değil → ekleme
                    }
                    break;
                }

                case HitStopPushType.Default:
                default:
                    // Add to queue
                    break;
            }

            _stops.Add(new HitStop { TargetScale = scale, EndUnscaledTime = end });

            // Hemen uygula (bir sonraki Update’i beklemeyelim)
            float newScale = Mathf.Min(_appliedScale, scale);
            if (!Mathf.Approximately(newScale, _appliedScale))
                ApplyTimeScale(newScale);
        }

        #endregion
        
        public override void Cleanup()
        {
            base.Cleanup();
            ClearHitStops();
        }
    }
}