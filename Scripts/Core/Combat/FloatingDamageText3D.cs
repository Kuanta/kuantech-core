using Kuantech.Core;

namespace Kuantech.Combat
{
    using System.Collections;
    using TMPro;
    using UnityEngine;

    namespace Kuantech.Core
    {
        public class FloatingDamageText3D : MonoBehaviour
        {
            [Header("References")]
            [SerializeField] private TMP_Text Text; // TextMeshPro (3D Object olanı, UI değil)

            [Header("Animation Settings")]
            [SerializeField] private float MoveSpeed = 2f; // Yukarı süzülme hızı
            [SerializeField] private float LifeTime = 1f; // Ne kadar ekranda kalacak
            [SerializeField] private AnimationCurve FadeCurve; // Opaklık eğrisi (Sonlara doğru azalsın)
            [SerializeField] private AnimationCurve ScaleCurve; // Büyüme/Küçülme eğrisi (Pop efekti için)

            [Header("Colors")]
            [SerializeField] private Color FriendlyColor = Color.green;
            [SerializeField] private Color EnemyColor = Color.white;
            [SerializeField] private Color CritColor = new Color(1f, 0.6f, 0f); // Turuncu
            
            [Header("Crit Settings")]
            [SerializeField] private float CritScaleMultiplier = 1.5f;

            [Header("Offset")]
            [SerializeField] private Vector3 RandomOffset = new Vector3(0.5f, 0.5f, 0f);

            private Camera _mainCam;
            private Transform _camTransform;
            private Vector3 _startPos;

            private void Awake()
            {
                _mainCam = Camera.main;
                if (_mainCam != null) _camTransform = _mainCam.transform;
                
                // Default eğriler yoksa kodla basit bir tane oluştur
                if (FadeCurve.length == 0) FadeCurve = AnimationCurve.Linear(0, 1, 1, 0); // 1'den 0'a
                if (ScaleCurve.length == 0) ScaleCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.2f, 1.2f), new Keyframe(1, 1)); // Pop efekti
            }

            public void Show(DamageInfo damageInfo, Vector3 worldPosition, bool isFriendly = false)
            {
                // 1. Pozisyonu ayarla (Hafif rastgelelik ile üst üste binmeyi önle)
                Vector3 offset = new Vector3(
                    Random.Range(-RandomOffset.x, RandomOffset.x),
                    Random.Range(-RandomOffset.y, RandomOffset.y),
                    Random.Range(-RandomOffset.z, RandomOffset.z)
                );
                transform.position = worldPosition + offset;
                _startPos = transform.position;

                // 2. Metni ve Rengi Ayarla
                Text.text = Mathf.RoundToInt(damageInfo.GetDamage()).ToString();

                if (damageInfo.IsCritical)
                {
                    Text.color = CritColor;
                    Text.fontSize = Text.fontSize * CritScaleMultiplier; // Veya scale ile oyna
                    Text.text += "!"; // Kritik efekt
                }
                else
                {
                    Text.color = isFriendly ? FriendlyColor : EnemyColor;
                    // Font size'ı resetlemeyi unutma (Pool kullandığın için)
                }

                // 3. Animasyonu Başlat
                StartCoroutine(AnimateRoutine(damageInfo.IsCritical));
            }

            // Metnin her zaman kameraya bakmasını sağlar (Billboard Effect)
            private void LateUpdate()
            {
                if (_camTransform != null)
                {
                    // LookAt kullanırsan text ters döner, o yüzden rotasyonu kameraya eşitliyoruz
                    transform.rotation = _camTransform.rotation;
                }
            }

            private IEnumerator AnimateRoutine(bool isCrit)
            {
                float timer = 0;
                Vector3 originalScale = Vector3.one * (isCrit ? CritScaleMultiplier : 1f);

                Color startColor = Text.color;

                while (timer < LifeTime)
                {
                    timer += Time.deltaTime;
                    float progress = timer / LifeTime;

                    // A. Yukarı Taşı
                    transform.position = _startPos + (Vector3.up * (MoveSpeed * timer));

                    // B. Scale Animasyonu (Pop Efekti)
                    float scaleEval = ScaleCurve.Evaluate(progress);
                    transform.localScale = originalScale * scaleEval;

                    // C. Fade Out (Şeffaflaşma)
                    float alphaEval = FadeCurve.Evaluate(progress);
                    Text.color = new Color(startColor.r, startColor.g, startColor.b, alphaEval);

                    yield return null;
                }

                // Bitti, havuza geri gönder
                PoolManager.PoolObject(gameObject);
            }
        }
    }
}