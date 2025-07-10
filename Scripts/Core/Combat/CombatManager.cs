using UnityEngine;

namespace Kuantech.Core
{
    public class CombatManager : SubManager
    {
        [Header("Damage Texts")] [SerializeField]
        private FloatingDamageText DamageTextPrefab;

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
        #endregion
        
    }
}