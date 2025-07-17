using System.Collections;
using Kuantech.Utils;
using TMPro;
using UnityEngine;

namespace Kuantech.Core
{
    public class FloatingDamageText : MonoBehaviour
    {
        [SerializeField] private TMP_Text Text;
        [SerializeField] private float DespawnDelay = 1;
        [SerializeField] private Animator Animator;

        [Header("Colors")] 
        [SerializeField] private bool AdjustColors;
        [SerializeField] private Color FriendlyColor;
        [SerializeField] private Color EnemyColor;
        
        private IEnumerator _routine;
        private static readonly int ShowHash = Animator.StringToHash("Show");

        public void Show(DamageInfo damageInfo, bool isFriendly = false)
        {
            if (Animator != null)
            {
                Animator.SetTrigger(ShowHash);
            }
            Text.text = damageInfo.DamageAmount.Stringfy(true);

            if (AdjustColors)
            {
                Text.color = isFriendly ? FriendlyColor : EnemyColor;
            }
            
            if (_routine != null)
            {
                StopCoroutine(_routine);
            }
        
            _routine = DespawnRoutine();
            StartCoroutine(_routine);
        }

        private IEnumerator DespawnRoutine()
        {
            yield return new WaitForSeconds(DespawnDelay);
            _routine = null;
            PoolManager.PoolObject(gameObject);
        }
    }
}