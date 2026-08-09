using System.Collections;
using Kuantech.Utils;
using TMPro;
using UnityEngine;

namespace Kuantech.Core
{
    /// <summary>
    /// A pooled damage number. Hot path: in a horde fight dozens of these are shown and recycled every
    /// second, so the show animation is driven here in code rather than by an Animator. An Animator costs a
    /// Rebind every time the object comes back out of the pool (SetActive) and an evaluated graph every
    /// frame it lives — for a one-second pop that is far more than the animation is worth.
    ///
    /// The animation only touches the transform. Animating colour or alpha would dirty the TMP mesh each
    /// frame and force the canvas to rebuild its batch; scale and position cost nothing on the canvas side.
    /// </summary>
    public class FloatingDamageText : MonoBehaviour
    {
        [SerializeField] private TMP_Text Text;
        [SerializeField] private float DespawnDelay = 1;

        [Header("Colors")]
        [SerializeField] private bool AdjustColors;
        [SerializeField] private Color FriendlyColor;
        [SerializeField] private Color EnemyColor;

        [Header("Crit")]
        [SerializeField] private Color CritColor = Color.yellow;
        [SerializeField] private GameObject CritIndicator;
        [SerializeField] private float CritScale = 1.2f;

        [Header("Show Animation")]
        [Tooltip("Scale punch at the start of the life, as a multiplier over the resting scale.")]
        [SerializeField] private float PunchScale = 1.35f;
        [Tooltip("Seconds the punch takes to settle back to the resting scale.")]
        [SerializeField] private float PunchDuration = 0.15f;
        [Tooltip("World units the number drifts upward over its whole life.")]
        [SerializeField] private float RiseDistance = 0.75f;
        [Tooltip("Fraction of the life spent shrinking away at the end. 0 disables the shrink-out.")]
        [Range(0f, 1f)]
        [SerializeField] private float ShrinkOutFraction = 0.25f;

        [Header("Placement")]
        [Tooltip("Rotate to face the camera on spawn so the number is readable. Baked once — fine for a fixed/iso camera.")]
        [SerializeField] private bool FaceCamera = true;
        [Tooltip("Lifts the text above the hit actor (world units).")]
        [SerializeField] private float HeightOffset = 1.5f;

        [Header("Offset")]
        [SerializeField] private Vector3 RandomOffsetMin = new Vector3(-0.1f, -0.1f, 0f);
        [SerializeField] private Vector3 RandomOffsetMax = new Vector3(0.1f, 0.1f, 0f);

        private IEnumerator _routine;
        private Transform _transform;
        // The authored colour, kept so a crit can be undone. Without this a pooled number that once showed a
        // crit stays crit-coloured for the rest of its life in the pool.
        private Color _defaultColor;
        // Resolved once per scene rather than per hit: GetCamera walks a manager lookup, and every number
        // shown in a wave would repeat it.
        private static UnityEngine.Camera _cachedCamera;

        private void Awake()
        {
            _transform = transform;
            if (Text != null) _defaultColor = Text.color;
        }

        public void Show(DamageInfo damageInfo, Actor owningActor)
        {
            Show(damageInfo.GetDamage(), owningActor, damageInfo.IsCritical);
        }

        public virtual void Show(float damageAmount, Actor owningActor, bool isCritical = false)
        {
            if (_transform == null) _transform = transform;

            // Setting the string re-parses and re-lays out the text. It is the one unavoidable TMP cost per
            // hit, so nothing else in this method is allowed to dirty the mesh again afterwards.
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

            if (CritIndicator != null)
            {
                CritIndicator.SetActive(isCritical);
            }

            Color wanted = isCritical ? CritColor : _defaultColor;
            // Only assign when it actually changes — writing the colour re-uploads the TMP vertex colours.
            if (Text.color != wanted) Text.color = wanted;

            //Lift above the actor + a little random scatter so stacked hits don't overlap perfectly.
            _transform.position += Vector3.up * HeightOffset + new Vector3(
                Random.Range(RandomOffsetMin.x, RandomOffsetMax.x),
                Random.Range(RandomOffsetMin.y, RandomOffsetMax.y),
                Random.Range(RandomOffsetMin.z, RandomOffsetMax.z));

            //Face the camera so the number is readable. Baked once here (cheap); fine for a fixed/iso camera.
            if (FaceCamera)
            {
                if (_cachedCamera == null) _cachedCamera = CameraManager.GetCamera();
                if (_cachedCamera != null) _transform.rotation = _cachedCamera.transform.rotation;
            }

            _routine = ShowRoutine(isCritical ? CritScale : 1f);
            StartCoroutine(_routine);
        }

        /// <summary>
        /// Punch out, drift up, shrink away, return to the pool — all on the transform, so the canvas never
        /// has to rebuild while the number is alive.
        /// </summary>
        private IEnumerator ShowRoutine(float restingScale)
        {
            Vector3 startPosition = _transform.position;
            float elapsed = 0f;
            float shrinkStart = DespawnDelay * (1f - ShrinkOutFraction);

            while (elapsed < DespawnDelay)
            {
                float scale = restingScale;

                if (elapsed < PunchDuration && PunchDuration > 0f)
                {
                    // Ease out of the punch so the number snaps in and settles rather than lerping flatly.
                    float t = elapsed / PunchDuration;
                    scale = restingScale * Mathf.Lerp(PunchScale, 1f, t * t);
                }
                else if (ShrinkOutFraction > 0f && elapsed > shrinkStart)
                {
                    float t = (elapsed - shrinkStart) / (DespawnDelay - shrinkStart);
                    scale = restingScale * (1f - t);
                }

                _transform.localScale = Vector3.one * scale;
                _transform.position = startPosition + Vector3.up * (RiseDistance * (elapsed / DespawnDelay));

                elapsed += Time.deltaTime;
                yield return null;
            }

            _transform.localScale = Vector3.one;
            _routine = null;
            PoolManager.PoolObject(gameObject);
        }
    }
}
