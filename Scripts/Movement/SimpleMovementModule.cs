using UnityEngine;

namespace Kuantech.Core
{
    /// <summary>
    /// Moves the actor by updating its transform (no rigidbody), smoothing the velocity so the
    /// motion accelerates, decelerates, and turns gradually instead of snapping to full speed.
    ///
    /// Knockback is treated as a physical impulse: a hit adds to the current velocity (preserving
    /// existing momentum) and self-propulsion is turned off while it bleeds off exponentially, so
    /// hits feel like a punchy shove that eases out rather than a flat linear push.
    /// </summary>
    public class SimpleMovementModule : ActorModule
    {
        [Tooltip("How quickly the current velocity approaches the desired velocity. " +
                 "Higher = snappier/more responsive, lower = floatier/smoother.")]
        public float Acceleration = 12f;

        [Header("Knockback")]
        [Tooltip("How quickly knockback velocity bleeds off. Higher = snappier recovery, lower = longer slide.")]
        public float KnockbackDamping = 6f;

        private MovementModule _movementModule;
        private Vector3 _velocity;
        private float _knockbackTimer;

        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            _movementModule = Actor.GetModule<MovementModule>();
            Actor.OnHitEvent -= OnHit;
            Actor.OnHitEvent += OnHit;
        }

        public override void ResetModule()
        {
            base.ResetModule();
            _velocity = Vector3.zero;
            _knockbackTimer = 0f;
        }

        private void OnHit(HitInfo hitInfo)
        {
            if (hitInfo.KnockbackForce <= 0f || hitInfo.KnockbackDuration <= 0f) return;

            Vector3 dir = hitInfo.HitDirection;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-6f) return;
            dir.Normalize();

            // Instantaneous impulse added on top of the current velocity, so existing momentum is
            // preserved: a fast-charging enemy hit with a small force keeps coming in, just slowed.
            _velocity += dir * hitInfo.KnockbackForce;
            _knockbackTimer = hitInfo.KnockbackDuration;
        }

        private void HandleMovement()
        {
            if (GameManager.IsGamePaused() || !Actor.IsAlive() || _movementModule == null)
            {
                return;
            }

            if (_knockbackTimer > 0f)
            {
                // Knockback active: self-propulsion is off. Coast on the impulse-laden velocity while
                // it decays exponentially — a sharp hit that eases out instead of a linear shove.
                _knockbackTimer -= Time.deltaTime;
                _velocity *= Mathf.Exp(-KnockbackDamping * Time.deltaTime);
            }
            else
            {
                // Normal locomotion: frame-rate independent smoothing toward the desired velocity.
                Vector3 desiredVelocity = _movementModule.GetMovementVector() * _movementModule.GetSpeed();
                float t = 1f - Mathf.Exp(-Acceleration * Time.deltaTime);
                _velocity = Vector3.Lerp(_velocity, desiredVelocity, t);
            }

            Actor.transform.position += _velocity * Time.deltaTime;
        }

        public override void ModuleUpdate()
        {
            HandleMovement();
        }
    }
}
