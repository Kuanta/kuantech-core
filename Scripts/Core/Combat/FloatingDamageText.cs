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

        [Header("Placement")]
        [Tooltip("Rotate to face the camera on spawn so the number is readable. Baked once — fine for a fixed/iso camera.")]
        [SerializeField] private bool FaceCamera = true;
        [Tooltip("Lifts the text above the hit actor (world units).")]
        [SerializeField] private float HeightOffset = 1.5f;

        [Header("Offset")]
        [SerializeField] private Vector3 RandomOffsetMin = new Vector3(-0.1f, -0.1f, 0f);
        [SerializeField] private Vector3 RandomOffsetMax = new Vector3(0.1f, 0.1f, 0f);
        private IEnumerator _routine;
        private static readonly int ShowHash = Animator.StringToHash("Show");

        public void Show(DamageInfo damageInfo, Actor owningActor)
        {

           Show(damageInfo.GetDamage(), owningActor, damageInfo.IsCritical);
        }

        public virtual void Show(float damageAmount, Actor owningActor, bool isCritical = false)
        {
            if (Animator != null)
            {
                Animator.SetTrigger(ShowHash);
            }
            Text.text = damageAmount.Stringfy(true);

            //todo: Find a better way 
            // if (AdjustColors)
            // {
            //     Text.color = isFriendly ? FriendlyColor : EnemyColor;
            // }

            if (_routine != null)
            {
                StopCoroutine(_routine);
            }

            _routine = DespawnRoutine();
            if (CritIndicator != null)
            {
                CritIndicator.SetActive(isCritical);
            }

            transform.localScale = Vector3.one;
            if (isCritical)
            {
                transform.localScale = Vector3.one * CritScale;
                Text.color = CritColor;
            }

            //Lift above the actor + a little random scatter so stacked hits don't overlap perfectly.
            transform.position += Vector3.up * HeightOffset + new Vector3(
                Random.Range(RandomOffsetMin.x, RandomOffsetMax.x),
                Random.Range(RandomOffsetMin.y, RandomOffsetMax.y),
                Random.Range(RandomOffsetMin.z, RandomOffsetMax.z));

            //Face the camera so the number is readable. Baked once here (cheap); fine for a fixed/iso camera.
            if (FaceCamera)
            {
                UnityEngine.Camera cam = CameraManager.GetCamera();
                if (cam != null) transform.rotation = cam.transform.rotation;
            }

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