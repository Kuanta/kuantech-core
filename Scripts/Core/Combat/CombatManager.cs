using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Kuantech.Core
{
    public class CombatManager : SubManager
    {
        [Header("Damage Texts")] [SerializeField]
        private FloatingDamageText DamageTextPrefab;
        [SerializeField]
        private FloatingDamageText HealTextPrefab;
        
        //Hit stop
        private class HitStop
        {
            public float TargetScale;
            public float EndUnscaledTime;
        }
        private readonly List<HitStop> _stops = new List<HitStop>();

        private float _baseFixedDeltaTime;
        private float _appliedScale = 1f;
        
        #region Damage Texts
        public static void ShowDamageText(Vector3 position, DamageInfo damageInfo, bool friendly)
        {
            CombatManager ctx = GetContext<CombatManager>();
            if (ctx == null) return;
            FloatingDamageText damageText =
                PoolManager.GetObjectFromPool(ctx.DamageTextPrefab.gameObject).GetComponent<FloatingDamageText>();
            if (damageText == null) return;
            damageText.transform.position = position;
            damageText.Show(damageInfo, friendly);
        }
        
        public static void ShowHealText(Vector3 position, DamageInfo healAmount, bool friendly)
        {
            CombatManager ctx = GetContext<CombatManager>();
            if (ctx == null) return;
            FloatingDamageText damageText =
                PoolManager.GetObjectFromPool(ctx.HealTextPrefab.gameObject).GetComponent<FloatingDamageText>();
            if (damageText == null) return;
            damageText.transform.position = position;
            damageText.Show(healAmount, friendly);
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
            float now = Time.unscaledTime;
            for (int i = _stops.Count - 1; i >= 0; i--)
            {
                if (now >= _stops[i].EndUnscaledTime)
                    _stops.RemoveAt(i);
            }
            float newScale = 1f;
            for (int i = 0; i < _stops.Count; i++)
                newScale = Mathf.Min(newScale, _stops[i].TargetScale);

            if (!Mathf.Approximately(newScale, _appliedScale))
                ApplyTimeScale(newScale);
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
        
        /// <summary>
        /// timeScale’i belirli süre (unscaled) için düşürür. 
        /// Örn: scale=0 => tam “hit-stop”, scale=0.1 => ağır çekim.
        /// </summary>
        public void PushHitStop(float scale, float durationSeconds)
        {
            scale = Mathf.Clamp(scale, 0f, 1f);
            float end = Time.unscaledTime + Mathf.Max(0f, durationSeconds);
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