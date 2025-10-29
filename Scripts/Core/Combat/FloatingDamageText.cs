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


        [Header("Crit")] [SerializeField] private Color CritColor = Color.yellow;
        [SerializeField] private GameObject CritIndicator;
        [SerializeField] private float CritScale = 1.2f;

        [Header("Offset")] 
        [SerializeField] private Vector3 RandomOffsetMin = new Vector3(-0.1f, -0.1f, 0f);
        [SerializeField] private Vector3 RandomOffsetMax = new Vector3(0.1f, 0.1f, 0f);
        private IEnumerator _routine;
        private static readonly int ShowHash = Animator.StringToHash("Show");

        public void Show(DamageInfo damageInfo, bool isFriendly = false)
        {
            if (Animator != null)
            {
                Animator.SetTrigger(ShowHash);
            }
            Text.text = damageInfo.GetDamage().Stringfy(true);

            if (AdjustColors)
            {
                Text.color = isFriendly ? FriendlyColor : EnemyColor;
            }
            
            if (_routine != null)
            {
                StopCoroutine(_routine);
            }
        
            _routine = DespawnRoutine();
            if (CritIndicator != null)
            {
                CritIndicator.SetActive(damageInfo.IsCritical);
            }

            transform.localScale = Vector3.one;
            if (damageInfo.IsCritical)
            {
                transform.localScale = Vector3.one * CritScale;
                Text.color = CritColor;
            }
            
            //Add random offset 
            transform.position += new Vector3(Random.Range(RandomOffsetMin.x, RandomOffsetMax.x),
                Random.Range(RandomOffsetMin.y, RandomOffsetMax.y), Random.Range(RandomOffsetMin.z, RandomOffsetMax.z));
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