using DG.Tweening;
using Kuantech.Core;
using Kuantech.Core.FX;
using Kuantech.Core.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Rpg
{
    /// <summary>
    /// A scattered loot orb. Physics-free: it arcs, bounces and settles via a scripted BallisticMotion
    /// (no Rigidbody, no colliders — cheap for lots of orbs), then idle-bobs until vacuumed to the player.
    /// The juice (arc, spin, land squash, bob) is all tweens/scripting, no physics needed.
    /// </summary>
    public class DropObject : MonoBehaviour
    {
        [Header("Vacuum")]
        public bool VacuumEnabled = true;
        public float CollectRange = 0.35f;

        [Header("Pickup On Touch")]
        [Tooltip("Collect this by walking into it rather than having it fly over. Needs a trigger collider " +
                 "on this object. Independent of vacuum — a drop can use either, both, or neither.")]
        public bool PickupOnTouch = false;
        [Tooltip("Layers allowed to pick this up by touching it.")]
        public LayerMask PickupLayers;

        [Header("Scatter")]
        [Tooltip("Initial launch speed (split into outward + upward by PitchAngle).")]
        public float ThrowForce = 5f;
        [Range(0f, 90f)] public float PitchAngle = 75f;
        public float SpawnHeightOffset = 0.3f;
        public float SpinSpeed = 360f;

        [Header("Scatter Ballistics")]
        public float Gravity = 25f;
        [Range(0f, 1f)] public float Bounciness = 0.5f;
        [Range(0f, 1f)] public float GroundFriction = 0.6f;
        public float SettleSpeed = 0.8f;

        [Header("Idle Bob")]
        public float BobHeight = 0.12f;
        public float BobDuration = 0.9f;

        [Header("Fx")]
        [SerializeField] private EffectPlayer CollectFx;

        public UnityAction<DropObject> OnCollectedEvent;

        private Transform _vacuumTarget;
        private float _vacuumSpeed;
        private bool _isScattering;
        private bool _isVacuuming;
        private bool _collected;
        private Tween _bobTween;
        private float _groundY;
        private BallisticMotion _motion;
        private float _spinSign = 1f;

        private Actor _claimedActor;
        protected virtual void OnEnable()
        {
            _isScattering = false;
            _isVacuuming = false;
            _collected = false;
            _vacuumTarget = null;
            _bobTween?.Kill();
        }

        protected virtual void OnDisable()
        {
            DOTween.Kill(transform);
            _bobTween?.Kill();
        }

        public void Scatter()
        {
            _isScattering = true;
            _isVacuuming = false;
            transform.localScale = Vector3.one;
            DOTween.Kill(transform);

            float groundY = transform.position.y;
            Vector3 start = transform.position + Vector3.up * SpawnHeightOffset;

            // Split the launch speed into an outward (XZ) and an upward part by the pitch angle.
            Vector2 xz = Random.insideUnitCircle.normalized;
            float rad = PitchAngle * Mathf.Deg2Rad;
            Vector3 horizontalVel = new Vector3(xz.x, 0f, xz.y) * (ThrowForce * Mathf.Cos(rad));
            float verticalVel = ThrowForce * Mathf.Sin(rad);

            _motion = new BallisticMotion(Gravity, Bounciness, GroundFriction, SettleSpeed);
            _motion.Launch(start, horizontalVel, verticalVel, groundY);
            _spinSign = Random.value > 0.5f ? 1f : -1f;
        }

        private void Update()
        {
            if (_collected) return;
            if (_isScattering) { UpdateScatter(); return; }
            if (_isVacuuming) UpdateVacuum();
        }

        private void UpdateScatter()
        {
            float dt = Time.deltaTime;
            _motion.Step(dt);
            transform.position = _motion.Position;
            if (SpinSpeed > 0f) transform.Rotate(0f, _spinSign * SpinSpeed * dt, 0f, Space.World);

            if (_motion.Settled) OnLanded();
        }

        private void OnLanded()
        {
            _isScattering = false;
            _groundY = _motion.Position.y;

            transform.DOPunchScale(new Vector3(0.3f, -0.2f, 0.3f), 0.3f, 5, 0.5f)
                .OnComplete(StartBob);
        }

        private void StartBob()
        {
            _bobTween?.Kill();
            _bobTween = transform
                .DOMoveY(_groundY + BobHeight, BobDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        public void BeginVacuum(LootVacuumModule vacuum, float speed)
        {
            if (!VacuumEnabled || _isScattering || _collected) return;
            if (_isVacuuming) return;
            _vacuumTarget = vacuum.Actor.transform;
            _vacuumSpeed = speed;
            _isVacuuming = true;
            _bobTween?.Kill();
            DOTween.Kill(transform);
            _claimedActor = vacuum.Actor;
        }

        private void UpdateVacuum()
        {
            if (_vacuumTarget == null)
            {
                _isVacuuming = false;
                return;
            }

            float dist = Vector3.Distance(transform.position, _vacuumTarget.position);
            float t = 1f - Mathf.Clamp01(dist / 4f);
            float speed = Mathf.Lerp(_vacuumSpeed, _vacuumSpeed * 3f, t);
            transform.position = Vector3.MoveTowards(transform.position, _vacuumTarget.position, speed * Time.deltaTime);

            if (dist <= CollectRange)
            {
                Collect();
            }
        }

        // Deliberately OnTriggerStay, not OnTriggerEnter: a drop can land right on top of the player while
        // still scattering, where PickUp refuses it. Enter would already have fired and never fire again,
        // leaving a coin sitting under the player forever. Stay re-offers it once the drop settles.
        private void OnTriggerStay(Collider other)
        {
            if (!PickupOnTouch || _collected || _isScattering) return;
            // An unset mask means "any layer" rather than "no layer" — otherwise forgetting to fill it in
            // silently disables pickup entirely, with nothing to see in the console.
            if (PickupLayers.value != 0 && (PickupLayers.value & (1 << other.gameObject.layer)) == 0) return;

            Actor collector = other.GetComponentInParent<Actor>();
            if (collector == null || !collector.IsAlive()) return;
            // Collecting loot is a capability, not a matter of which team you are on: an actor picks drops
            // up only if it is a looter. Enemies walk straight over them without a LootVacuumModule.
            if (collector.GetModule<LootVacuumModule>() == null) return;

            _claimedActor = collector;
            PickUp();
        }

        public void PickUp()
        {
            if (_isScattering) return;
            Collect();
        }

        private void Collect()
        {
            if (_collected) return;
            _collected = true;
            OnCollect(_claimedActor);
            OnCollectedEvent?.Invoke(this);
            PoolManager.PoolObject(gameObject);
        }

        protected virtual void OnCollect(Actor claimedActor) { }
    }
}
