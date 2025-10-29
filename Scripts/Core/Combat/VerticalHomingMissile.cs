using Kuantech.Rpg.Inventory;
using UnityEngine;

namespace Kuantech.Core
{
    /// <summary>
    /// Two-phase missile:
    /// (1) Vertical launch along a configurable up vector (default: WorldUp)
    /// (2) Homing toward the target with angular speed limit
    ///
    /// Dimension-agnostic:
    /// - No plane projection, no CancelUpComponent
    /// - Works in 3D out of the box
    /// - Works in 2D if your scene/gameplay is on a plane (e.g., XY) and your prefab forward is set correctly
    /// 
    /// Requirements from base Projectile:
    /// - Uses base fields: WorldUp, WorldForward, TurnRateDegPerSec, ReachThreshold, etc.
    /// - Uses base methods: GetForwardRotation(worldDir), HandleOnTriggerEnter(...), CheckLifetime(), Despawn()
    /// </summary>
    public class VerticalLaunchHomingProjectile : Projectile
    {
        private enum FlightPhase { LaunchUp, Homing }

        [Header("Vertical Launch")]
        [Tooltip("Enable the initial vertical launch phase before homing.")]
        public bool UseVerticalLaunch = true;

        [Tooltip("Up direction used during launch. If near-zero, WorldUp is used.")]
        public Vector3 LaunchUpDirection = Vector3.up;

        [Tooltip("Time gate for the launch phase (seconds). Set 0 to disable this gate.")]
        public float LaunchUpSeconds = 0.35f;

        [Tooltip("Distance gate for the launch phase (meters). Set 0 to disable this gate.")]
        public float LaunchUpDistance = 2.5f;

        [Tooltip("Speed multiplier during the launch phase (relative to Speed).")]
        public float LaunchSpeedMultiplier = 1.0f;

        [Tooltip("Scale turning rate while launching (0..1). 1 = unchanged.")]
        public float TurnRateScaleDuringLaunch = 0.6f;

        [Tooltip("Optional forward boost when switching to homing (meters per second). 0 = none.")]
        public float HandoverBoost = 0f;

        public float TurnRateDegPerSec = 360.0f;
        
        // Runtime
        private FlightPhase _phase = FlightPhase.Homing;
        private float _launchElapsed = 0f;
        private float _launchTravel = 0f;
        private Vector3 _launchDir = Vector3.up;   // normalized
        private Vector3 _lastPos;

        public override void Shoot(Actor castBy, Weapon shotFrom, Vector3 shootPosition, Vector3 shootDirection, Transform target = null, float relativeSpeed = 0.0f)
        {
            base.Shoot(castBy, shotFrom, shootPosition, shootDirection, target, relativeSpeed);

            // Initialize phase state
            _launchElapsed = 0f;
            _launchTravel  = 0f;
            _lastPos       = transform.position;

            // Choose launch direction (prefer explicit LaunchUpDirection, fallback to WorldUp)
            _launchDir = (LaunchUpDirection.sqrMagnitude > 1e-6f ? LaunchUpDirection : WorldUp).normalized;

            // Enter LaunchUp only if enabled, there is a target to home later, and at least one gate is set
            bool hasGate = (LaunchUpSeconds > 0f) || (LaunchUpDistance > 0f);
            _phase = (UseVerticalLaunch && target != null && hasGate) ? FlightPhase.LaunchUp : FlightPhase.Homing;

            // During launch, force heading to the launch direction (no steering yet)
            if (_phase == FlightPhase.LaunchUp)
            {
                _direction = _launchDir;
                transform.rotation = GetForwardRotation(_direction);
            }
            else
            {
                // If starting directly in Homing, align initial heading toward target if available
                if (Target != null)
                {
                    Vector3 toTarget = (GetTargetPosition() - transform.position);
                    if (toTarget.sqrMagnitude > 1e-8f)
                    {
                        _direction = toTarget.normalized;
                        transform.rotation = GetForwardRotation(_direction);
                    }
                }
            }
        }

        protected override void Update()
        {
            if (Despawned) return;

            // If target vanished (when homing) or speed is zero, despawn like base
            if ((Target != null && (!Target.gameObject.activeInHierarchy)) || CurrentSpeed <= 0f)
            {
                Despawn();
                return;
            }

            float dt = Time.deltaTime;

            switch (_phase)
            {
                case FlightPhase.LaunchUp:
                    TickLaunchUp(dt);
                    break;

                case FlightPhase.Homing:
                    TickHoming(dt);
                    break;
            }

            CheckLifetime();
        }

        private void TickLaunchUp(float dt)
        {
            // 1) Move purely along the launch up direction
            float moveSpeed = CurrentSpeed * Mathf.Max(0f, LaunchSpeedMultiplier);
            Vector3 prev    = transform.position;
            Vector3 delta   = _launchDir * moveSpeed * dt;

            transform.position = prev + delta;

            // 2) Face by actual velocity (keeps the visual always visible and aligned with motion)
            Vector3 vel = transform.position - prev;
            if (vel.sqrMagnitude > 1e-10f)
            {
                transform.rotation = GetForwardRotation(vel.normalized);
            }

            // 3) Accumulate launch phase gates
            _launchElapsed += dt;
            _launchTravel  += delta.magnitude;
            _lastPos        = transform.position;

            bool timeGateMet = (LaunchUpSeconds > 0f)   && (_launchElapsed >= LaunchUpSeconds);
            bool distGateMet = (LaunchUpDistance > 0f)  && (_launchTravel  >= LaunchUpDistance);

            if (timeGateMet || distGateMet)
            {
                // Optional forward boost at the handover (use current forward/velocity direction)
                if (HandoverBoost > 0f && vel.sqrMagnitude > 1e-10f)
                {
                    Vector3 fwd = vel.normalized;
                    transform.position += fwd * (HandoverBoost * dt);
                    _direction = fwd;
                }

                // Switch to homing and snap heading toward target for a crisp handover
                _phase = FlightPhase.Homing;

                if (Target != null && Target.gameObject.activeInHierarchy)
                {
                    Vector3 toTarget = (GetTargetPosition() - transform.position);
                    if (toTarget.sqrMagnitude > 1e-8f)
                        _direction = toTarget.normalized;
                }
            }
        }

        private void TickHoming(float dt)
        {
            // 1) Steer toward target with angular speed limit (stable homing)
            if (Target != null && Target.gameObject.activeInHierarchy)
            {
                Vector3 toTarget = (GetTargetPosition() - transform.position);
                if (toTarget.sqrMagnitude > 1e-8f)
                {
                    float turnScale  = (_phase == FlightPhase.LaunchUp) ? Mathf.Clamp01(TurnRateScaleDuringLaunch) : 1f;
                    float maxRadians = Mathf.Deg2Rad * Mathf.Max(0f, TurnRateDegPerSec) * turnScale * dt;
                    Vector3 desired  = toTarget.normalized;

                    _direction = Vector3.RotateTowards(_direction, desired, maxRadians, float.PositiveInfinity);
                    _direction.Normalize();
                }
            }

            // 2) Move along current heading
            Vector3 prev = transform.position;
            transform.position = prev + _direction * (CurrentSpeed * dt);

            // 3) Face forward by velocity (fallback to heading if velocity is tiny)
            Vector3 vel = transform.position - prev;
            if (vel.sqrMagnitude > 1e-10f)
                transform.rotation = GetForwardRotation(vel.normalized);
            else
                transform.rotation = GetForwardRotation(_direction);

            // 4) Reach check
            if (Target != null && Target.gameObject.activeInHierarchy)
            {
                Vector3 diff = (GetTargetPosition() - transform.position);
                if (diff.sqrMagnitude <= ReachThreshold * ReachThreshold)
                {
                    HandleOnTriggerEnter(Target.gameObject);
                    Despawn();
                }
            }
        }
    }
}
