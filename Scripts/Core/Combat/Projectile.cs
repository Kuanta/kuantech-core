using System.Collections.Generic;
using Kuantech.Core.Combat;
using Kuantech.Core.FX;
using Kuantech.Rpg.Inventory;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Core
{
    public class Projectile : MonoBehaviour
    {
         [Header("World Axes")]
        public Vector3 WorldForward = Vector3.forward; // Forward axis of the model (default Unity +Z)
        public Vector3 WorldUp = Vector3.up;
        [Tooltip("World right. Needed for arc calculations")]
        public Vector3 WorldRight = Vector3.right;

        [Header("Properties")]
        public string ProjectileId;
        public bool Is2D = false;
        public float Speed = 10f;
        public float Range = 10f;
        public float RotationSlerpFactor = 1000f;

        [Header("Arc")] 
        public float InitialRiseHeight;
        public float Gravity = 10f;
        public bool RequireReachPeakForImpact = true;
        [Tooltip("If rise velocity smaller than this, projectile is considered to be reached the peak")]
        public float RiseVelocityReachedPeakThresh = 0.2f;
        
        [Tooltip("Squared distance threshold to consider target reached")]
        public float ReachThreshold = 0.1f;

        [Tooltip("Maximum lifetime of projectile in seconds")]
        public float MaxLifetime = 20f;
        
        public float Knockback = 0f;
        public float KnockbackTime = 0f;
        
        [Header("Damage")]
        public bool RawDamage = false;
        public DamageInfo Damage;
        public float SplashRadius = 0f; // 0 = no splash
        public DamageInfo SplashDamage;

        [Header("Ownership & Filters")]
        public Actor CastBy;
        public Weapon ShotFrom = null;
        public LayerMask Targets;
        public HashSet<int> FactionFilter;

        [Header("Visuals & Colliders")]
        public GameObject Visual;
        public TrailRenderer TrailRenderer;
        public Collider Collider;
        public Collider2D Collider2D;

        [Header("Effects")]
        public EffectPlayer StartEffect;
        public EffectPlayer LifetimeEndEffect;
        public EffectPlayer ImpactEffect;

        [Header("Pooling")]
        public bool DestroyOnImpact = true;
        public float DespawnDelay = 0f;

        // Events
        public UnityAction<Projectile> ShotEvent;
        public UnityAction<Projectile> LifetimeEndEvent;
        public UnityAction<Projectile> OnImpactEvent;

        // Runtime state
        protected bool Despawned = false;
        protected float _age = 0f;
        protected float _lifeTime = 0f;
        protected float CurrentSpeed = 0f;
        protected Vector3 _direction;
        protected Vector3 _currentBasePosition; //Current position without arc calculations
        protected Vector3 _targetOffset = Vector3.zero;
        
        //Arc runtime
        private bool _useArc;
        private bool _reachedPeak;
        private Vector3 _currentRiseHeight;
        private Vector3 _initialRiseVelocity;
        private Vector3 _currentRiseVelocity;

        // Target
        protected Transform Target;
        public delegate void ImpactOverrideDelegate(Projectile proj, Actor target, GameObject hitGO);
        public ImpactOverrideDelegate ImpactOverride;

        // Attachments
        public List<GameObject> Attachments = new List<GameObject>();

        #region Helpers
        // Horizontal component (plane perpendicular to WorldUp)
        private Vector3 Planar(Vector3 v)
        {
            Vector3 up = (WorldUp.sqrMagnitude > 1e-6f ? WorldUp.normalized : Vector3.up);
            return v - Vector3.Dot(v, up) * up;
        }

        // Unit up
        private Vector3 UpUnit()
        {
            return (WorldUp.sqrMagnitude > 1e-6f ? WorldUp.normalized : Vector3.up);
        }

        // Desired flight direction (FULL 3D, normalized)
        protected Vector3 GetFlightDirection()
        {
            if (Target != null)
            {
                Vector3 diff = (GetTargetPosition() - transform.position);
                if (diff.sqrMagnitude > 1e-8f) return diff.normalized;
            }
            return _direction.normalized; // fallback to current heading
        }
        private Vector3 _lastUp = Vector3.zero;
        private bool _hasLastUp = false;

        protected Quaternion GetForwardRotation(Vector3 worldDir)
        {
            // 1) Normalize inputs and set fallbacks
            Vector3 f  = (worldDir.sqrMagnitude > 1e-6f ? worldDir : WorldForward).normalized;
            Vector3 upRef = (WorldUp.sqrMagnitude  > 1e-6f ? WorldUp  : Vector3.up).normalized;

            // 2) Build a stable right using a reference axis mostly orthogonal to f
            //    Prefer WorldUp when possible; fall back to a cardinal axis if too parallel.
            Vector3 right = Vector3.Cross(upRef, f);
            if (right.sqrMagnitude < 1e-6f) // upRef ~ f  (near parallel)
            {
                // pick the world axis least aligned with f to avoid degeneracy
                Vector3 axis =
                    (Mathf.Abs(f.x) <= Mathf.Abs(f.y) && Mathf.Abs(f.x) <= Mathf.Abs(f.z)) ? Vector3.right :
                    (Mathf.Abs(f.y) <= Mathf.Abs(f.z)) ? Vector3.up : Vector3.forward;

                right = Vector3.Cross(axis, f);
                if (right.sqrMagnitude < 1e-8f) // ultra-degenerate fallback
                    right = Vector3.Cross(Vector3.right, f);
            }
            right.Normalize();

            // 3) Compute up from f × right (right-handed basis)
            Vector3 up = Vector3.Cross(f, right);
            up.Normalize();

            // 4) Hemisphere continuity: keep 'up' close to previous up to avoid flips
            if (_hasLastUp)
            {
                if (Vector3.Dot(up, _lastUp) < 0f)
                {
                    // Flip both to keep a right-handed frame and preserve continuity
                    up    = -up;
                    right = -right;
                }
            }
            _lastUp = up;
            _hasLastUp = true;

            // 5) Compose final rotation (Unity +Z to f, 'up' stabilized)
            Quaternion look = Quaternion.LookRotation(f, up);

            // 6) Map your model's forward axis to Unity +Z
            Vector3 modelFwd = (WorldForward.sqrMagnitude > 1e-6f ? WorldForward : Vector3.forward).normalized;
            Quaternion modelToPlusZ = Quaternion.FromToRotation(modelFwd, Vector3.forward);

            return look * modelToPlusZ;
        }

        protected Vector3 GetTargetPosition()
        {
            if (Target == null) return Vector3.zero;
            return Target.position + _targetOffset;
        }

        public void SetTargetOffset(Vector3 targetOffset)
        {
            _targetOffset = targetOffset;
        }

        #endregion

        
        #region Shoot

        public virtual void Shoot(Actor castBy, Weapon shotFrom, Vector3 shootPosition, Vector3 shootDirection, Transform target = null, float relativeSpeed = 0.0f)
        {
            // Set faction enemies
            if (castBy != null)
                FactionFilter = castBy.FactionHandler.GetEnemyFactions().ToHashSet();
            else
                FactionFilter = new HashSet<int>();
            
            _currentBasePosition = shootPosition;
            _targetOffset = Vector3.zero;

            if (InitialRiseHeight > 0) _useArc = true;
            
            //Calculate initial rise velocity
            _initialRiseVelocity = UpUnit() * Mathf.Sqrt(Mathf.Max(0f, 2f * Gravity * InitialRiseHeight));
            _currentRiseVelocity = _initialRiseVelocity;

            Reset();


            _direction = shootDirection;
            transform.position = shootPosition;
            transform.rotation = GetForwardRotation(_direction);

            if (Visual != null) Visual.transform.localScale = Vector3.one;

            CastBy = castBy;
            ShotFrom = shotFrom;
            ImpactOverride = null;
            DestroyOnImpact = true;
            CurrentSpeed = Speed + relativeSpeed;

            if (StartEffect != null) StartEffect.PlayEffectAtPosition(transform.position, Quaternion.identity);
            ShotEvent?.Invoke(this);

            // Target setup
            Target = target;

            // Lifetime
            if (Target != null)
            {
                // Homing: keep alive until MaxLifetime
                _lifeTime = MaxLifetime;
            }
            else
            {
                // Straight: based on range / speed
                _lifeTime = Range / Mathf.Max(0.0001f, CurrentSpeed);
            }

            Despawned = false;
            ToggleCollider(true);
            if (Visual != null) Visual.SetActive(true);
            if (TrailRenderer != null) TrailRenderer.Clear();
        }


        // ==========================
        // Update (movement + facing)
        // ==========================
       protected virtual void Update()
        {
            if (Despawned) return;
            if ((Target != null && (!Target.gameObject.activeInHierarchy)) || CurrentSpeed <= 0f)
            {
                Despawn(); return;
            }

            float dt = Time.deltaTime;
            _age += dt;

            if (_useArc)
            {
                //Calculate arc height
                _currentRiseHeight += _currentRiseVelocity * Time.deltaTime;
                _currentRiseVelocity -= WorldUp * Gravity * Time.deltaTime;

                //Clamp current rise height to 0 if its in the inverse direction of world up
                if (Vector3.Dot(_currentRiseHeight, WorldUp) < 0f && _reachedPeak)
                {
                    _currentRiseHeight = Vector3.zero;
                    _currentRiseVelocity = Vector3.zero;
                }
            }
            else
            {
                _currentRiseHeight = Vector3.zero;
            }
      
            
            _currentBasePosition += GetFlightDirection() * CurrentSpeed * dt;
            Vector3 prevPos = transform.position;
            Vector3 newPos = _currentBasePosition + _currentRiseHeight;
            transform.position = newPos;
            Vector3 finalMovementVector = (newPos - prevPos).normalized;
            
            //Check reached peak
            if (_useArc && !_reachedPeak )
            {
                Vector3 posDiff = newPos - prevPos;
                bool velDown = Vector3.Dot(posDiff, WorldUp) <= 0;
                if (velDown)
                {
                    _reachedPeak = true;
                }
            }
            
            // Face travel direction (stable): prefer horizontal component to avoid pitching in top-down,
            // otherwise use total velocity if you want true 3D nose-up/down.
            if (finalMovementVector.sqrMagnitude > 1e-8f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    GetForwardRotation(finalMovementVector),
                    RotationSlerpFactor * dt
                );
            }

            if (Target != null && Target.gameObject.activeInHierarchy)
            {
                Vector3 diff = GetTargetPosition() - transform.position;
                if (diff.sqrMagnitude <= ReachThreshold * ReachThreshold)
                {
                    HandleOnTriggerEnter(Target.gameObject);
                    Despawn();
                    return;
                }
            }

            CheckLifetime();
        }
        #endregion

        
        #region Lifecycle

        protected void CheckLifetime()
        {
            if (Despawned) return;
            if (_age > Mathf.Min(_lifeTime, MaxLifetime))
            {
                EndLifetime();
            }
        }

        private void EndLifetime()
        {
            LifetimeEndEvent?.Invoke(this);
            if (LifetimeEndEffect != null) LifetimeEndEffect.PlayEffectAtPosition(transform.position, Quaternion.identity);
            Despawn();
        }

        public virtual void Despawn()
        {
            if (Despawned) return;

            Despawned = true;
            _age = 0f;

            ClearAttachments();

            if (DespawnDelay <= 0f)
            {
                PoolManager.PoolObject(gameObject);
                return;
            }

            ToggleCollider(false);
            if (Visual != null) Visual.SetActive(false);
            PoolManager.PoolObject(gameObject, DespawnDelay);
        }

        public void Reset()
        {
            if (Visual != null) Visual.transform.localScale = Vector3.one;
            Despawned = false;
            _age = 0f;
            _reachedPeak = false;
            _currentRiseHeight = Vector3.zero;
        }

        #endregion

        #region Attachments

        public void AddAttachment(GameObject component)
        {
            Attachments.Add(component);
            component.transform.SetParent(transform);
            component.transform.localPosition = Vector3.zero;
            component.transform.localRotation = Quaternion.identity;
        }

        public void AddEffect(int effectType)
        {
            Effect effect = EffectsLibrary.GetContext<EffectsLibrary>().PlayEffect(effectType, Vector3.zero, Quaternion.identity);
            AddAttachment(effect.gameObject);
            effect.transform.localPosition = Vector3.zero;
            effect.transform.localRotation = Quaternion.identity;
        }

        private void ClearAttachments()
        {
            for (int i = 0; i < Attachments.Count; i++)
            {
                PoolManager.PoolObject(Attachments[i]);
            }
            Attachments.Clear();
        }

        #endregion

        #region Impact

        // ==========================
        // Colliders / Impact
        // ==========================
        public void ToggleCollider(bool toggle)
        {
            if (Collider != null)   Collider.enabled = toggle;
            if (Collider2D != null) Collider2D.enabled = toggle;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!((Targets.value & (1 << other.gameObject.layer)) > 0)) return;
            HandleOnTriggerEnter(other.gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!((Targets.value & (1 << other.gameObject.layer)) > 0)) return;
            HandleOnTriggerEnter(other.gameObject);
        }

        // ==========================
        // Impact Handling
        // ==========================
        protected virtual void HandleOnTriggerEnter(GameObject triggeredObject)
        {
            if (CastBy != null && triggeredObject == CastBy.gameObject) return;

            Actor targetActor = triggeredObject.GetComponent<Actor>();
            if (targetActor != null && (!targetActor.IsAlive())) return;

            if (CastBy != null && CastBy.IsAlly(targetActor))
            {
                return;
            }
            
            if (_useArc && !_reachedPeak && RequireReachPeakForImpact)
            {
                Debug.LogError("Not reached peak");
                return; //Wait for peak
            }

            if (ImpactEffect != null) ImpactEffect.PlayEffectAtPosition(transform.position, Quaternion.identity);
            OnImpactEvent?.Invoke(this);

            if (ImpactOverride != null)
            {
                ImpactOverride(this, targetActor, triggeredObject);
                return;
            }

            if (SplashRadius > 0f)
            {
                Vector3 origin = transform.position;

                if (Is2D)
                {
                    HitInfo hitInfo2D = new HitInfo()
                    {
                        DamageInfo = SplashDamage,
                        Hitter = CastBy != null ? CastBy.gameObject : null,
                        HitDirection = _direction,
                        KnockbackDuration = KnockbackTime,
                        KnockbackForce = Knockback,
                    };
                    CombatUtilities.HitActorsInCircle2D(origin, SplashRadius, Targets, hitInfo2D, FactionFilter);
                }
                else
                {
                    Collider[] colliders3D = UnityEngine.Physics.OverlapSphere(origin, SplashRadius);
                    foreach (Collider coll in colliders3D)
                    {
                        Impact(coll.gameObject);
                    }
                }
            }
            else
            {
                Impact(triggeredObject);
            }

            if (DestroyOnImpact)
            {
                Despawn();
            }
        }

        protected virtual void Impact(GameObject impacted)
        {
            if (DestroyOnImpact && Despawned) return;

            Actor target = impacted.GetComponent<Actor>();
            GameObject hitter = CastBy != null ? CastBy.gameObject : null;

            if (target != null)
            {
                target.OnHit(new HitInfo()
                {
                    DamageInfo = Damage,
                    Hitter = hitter,
                    HitDirection = _direction,
                    KnockbackDuration = KnockbackTime,
                    KnockbackForce = Knockback,
                });
            }
        }

        #endregion
        
    }
}