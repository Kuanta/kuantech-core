using System.Collections.Generic;
using System.Linq;
using Kuantech.Core.FX;
using Kuantech.Core.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Core
{
    /// <summary>
    /// A thrown object that arcs to a target POINT and goes off where it lands — grenades, molotovs,
    /// bottles. Unlike <see cref="Projectile"/>, which flies along a direction until it touches something,
    /// a throwable is solved to land exactly on the point it was aimed at, and carries no collider in
    /// flight (so it can't detonate mid-air on someone it clips on the way).
    ///
    /// Flight is a scripted <see cref="BallisticMotion"/> — no Rigidbody, no physics queries until impact.
    /// Impact is a single overlap sphere; override <see cref="OnHitEnemy"/> to add a payload on top of the
    /// damage (a burn status effect, a slow, ...). Plain damage + knockback needs no subclass at all.
    /// </summary>
    public class Throwable : MonoBehaviour
    {
        // Shared across all throwables — impact is resolved within a single frame, so one buffer is safe.
        private static readonly Collider[] OverlapBuffer = new Collider[64];

        [Header("Flight")]
        [Tooltip("Time to reach the target point, whatever the distance. Fixed time keeps the throw readable at every range.")]
        public float FlightTime = 0.8f;
        [Tooltip("Downward acceleration in flight. Higher = tighter, snappier arc for the same flight time.")]
        public float Gravity = 25f;
        [Tooltip("Ground bounces to take before going off. 0 = detonate the moment it lands.")]
        public int BouncesBeforeImpact = 0;
        [Range(0f, 1f)] public float Bounciness = 0.4f;
        [Range(0f, 1f)] public float GroundFriction = 0.5f;
        [Tooltip("Below this speed the throwable is considered to have come to rest and goes off.")]
        public float SettleSpeed = 0.5f;
        [Tooltip("Degrees per second of tumble while in the air. Purely cosmetic.")]
        public float SpinSpeed = 540f;
        [Tooltip("Safety net: go off anyway if it somehow never lands (thrown off the map, etc).")]
        public float MaxLifetime = 6f;

        [Header("Impact")]
        public float ImpactRadius = 3f;
        public LayerMask Targets;
        public DamageInfo Damage;
        [Tooltip("Knockback pushed radially outward from the blast centre.")]
        public float Knockback = 0f;
        public float KnockbackDuration = 0.2f;

        [Header("Visuals & Effects")]
        public GameObject Visual;
        public EffectPlayer ThrowEffect;
        public EffectPlayer BounceEffect;
        public EffectPlayer ImpactEffect;

        [Header("Pooling")]
        [Tooltip("Delay before returning to the pool, so impact effects can finish. The visual is hidden immediately.")]
        public float DespawnDelay = 0f;

        public UnityAction<Throwable> OnImpactEvent;

        //Runtime
        protected Actor ThrownBy;
        protected HashSet<int> FactionFilter;

        private BallisticMotion _motion;
        private Vector3 _spinAxis;
        private int _bounceCount;
        private float _age;
        private bool _flying;
        private bool _impacted;

        /// <summary>
        /// Throws this at a world point. The arc is solved so it arrives there after <see cref="FlightTime"/>
        /// seconds — the target point's height becomes the ground plane it lands on.
        /// </summary>
        public virtual void Throw(Actor thrownBy, Vector3 startPosition, Vector3 targetPoint)
        {
            ThrownBy = thrownBy;
            FactionFilter = thrownBy != null
                ? thrownBy.FactionHandler.GetEnemyFactions().ToHashSet()
                : new HashSet<int>();

            float flightTime = Mathf.Max(0.05f, FlightTime);
            Vector3 delta = targetPoint - startPosition;

            // Solve the launch velocity for "be at targetPoint after flightTime":
            //   horizontal is just distance/time; vertical must also cancel the drop gravity causes.
            Vector3 horizontalVelocity = new Vector3(delta.x, 0f, delta.z) / flightTime;
            float verticalVelocity = delta.y / flightTime + 0.5f * Gravity * flightTime;

            _motion = new BallisticMotion(Gravity, Bounciness, GroundFriction, SettleSpeed);
            _motion.Launch(startPosition, horizontalVelocity, verticalVelocity, targetPoint.y);

            // Tumble end-over-end around the axis perpendicular to travel.
            _spinAxis = Vector3.Cross(horizontalVelocity, Vector3.up);
            if (_spinAxis.sqrMagnitude < 1e-6f) _spinAxis = Vector3.right;
            _spinAxis.Normalize();

            _bounceCount = 0;
            _age = 0f;
            _impacted = false;
            _flying = true;

            transform.position = startPosition;
            if (Visual != null) Visual.SetActive(true);
            if (ThrowEffect != null) ThrowEffect.PlayEffectAtPosition(startPosition, Quaternion.identity);
        }

        protected virtual void Update()
        {
            if (!_flying) return;

            float dt = Time.deltaTime;
            _age += dt;

            bool bounced = _motion.Step(dt);
            transform.position = _motion.Position;
            if (SpinSpeed > 0f) transform.Rotate(_spinAxis, SpinSpeed * dt, Space.World);

            if (bounced)
            {
                _bounceCount++;
                if (BounceEffect != null) BounceEffect.PlayEffectAtPosition(transform.position, Quaternion.identity);
                if (_bounceCount > BouncesBeforeImpact)
                {
                    Impact();
                    return;
                }
            }

            // Came to rest without spending its bounces (soft ground tuning), or never landed at all.
            if (_motion.Settled || _age >= MaxLifetime) Impact();
        }

        #region Impact

        /// <summary>
        /// Goes off: damages and knocks back every valid enemy in <see cref="ImpactRadius"/>, then despawns.
        /// </summary>
        protected virtual void Impact()
        {
            if (_impacted) return;
            _impacted = true;
            _flying = false;

            Vector3 origin = transform.position;
            if (ImpactEffect != null) ImpactEffect.PlayEffectAtPosition(origin, Quaternion.identity);

            if (ImpactRadius > 0f) HitEnemiesInRadius(origin);

            OnImpactEvent?.Invoke(this);

            Despawn();
        }

        // Own overlap instead of CombatUtilities.HitActorsInSphere: a blast needs a per-target hit direction
        // (radially outward) so knockback throws bodies away from the centre rather than all the same way.
        private void HitEnemiesInRadius(Vector3 origin)
        {
            int count = UnityEngine.Physics.OverlapSphereNonAlloc(origin, ImpactRadius, OverlapBuffer, Targets);
            GameObject hitter = ThrownBy != null ? ThrownBy.gameObject : gameObject;

            for (int i = 0; i < count; i++)
            {
                Collider col = OverlapBuffer[i];
                if (col == null) continue;
                if (!col.TryGetComponent(out Actor actor)) continue;
                if (!actor.IsAlive() || actor == ThrownBy) continue;
                if (FactionFilter != null && FactionFilter.Count > 0 && !FactionFilter.Contains(actor.GetFactionId())) continue;

                // Radially outward on the ground plane. Standing exactly on the blast centre is degenerate,
                // so pick a random horizontal direction rather than dividing by zero.
                Vector3 hitDirection = actor.GetActorLocation() - origin;
                hitDirection.y = 0f;
                if (hitDirection.sqrMagnitude > 1e-6f) hitDirection.Normalize();
                else
                {
                    Vector2 random = Random.insideUnitCircle.normalized;
                    hitDirection = new Vector3(random.x, 0f, random.y);
                }

                actor.OnHit(new HitInfo
                {
                    DamageInfo = Damage,
                    Hitter = hitter,
                    HitDirection = hitDirection,
                    KnockbackForce = Knockback,
                    KnockbackDuration = KnockbackDuration,
                });

                OnHitEnemy(actor);
            }
        }

        /// <summary>
        /// Called once per enemy caught in the blast, after the damage and knockback have been applied.
        /// Override to add this throwable's payload — a burn status effect for a molotov, a slow for an
        /// ice flask, and so on. The base throwable already covers plain damage + knockback.
        /// </summary>
        protected virtual void OnHitEnemy(Actor enemy) { }

        #endregion

        #region Lifecycle

        public virtual void Despawn()
        {
            _flying = false;
            if (DespawnDelay <= 0f)
            {
                PoolManager.PoolObject(gameObject);
                return;
            }

            if (Visual != null) Visual.SetActive(false);
            PoolManager.PoolObject(gameObject, DespawnDelay);
        }

        protected virtual void OnDisable()
        {
            _flying = false;
            _impacted = false;
            OnImpactEvent = null;
            ThrownBy = null;
        }

        #endregion
    }
}
