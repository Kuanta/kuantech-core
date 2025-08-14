using UnityEngine;

namespace Kuantech.Core
{
    public class CombatManager : SubManager
    {
        [Header("Damage Texts")] [SerializeField]
        private FloatingDamageText DamageTextPrefab;
        [SerializeField]
        private FloatingDamageText HealTextPrefab;
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
        
    }
}