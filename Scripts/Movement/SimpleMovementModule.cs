using UnityEngine;

namespace Kuantech.Core
{
    /// <summary>
    /// Moves the actor by updating its transform (no rigidbody), smoothing the velocity so the
    /// motion accelerates, decelerates, and turns gradually instead of snapping to full speed.
    /// </summary>
    public class SimpleMovementModule : ActorModule
    {
        [Tooltip("How quickly the current velocity approaches the desired velocity. " +
                 "Higher = snappier/more responsive, lower = floatier/smoother.")]
        public float Acceleration = 12f;

        private MovementModule _movementModule;
        private Vector3 _velocity;

        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            _movementModule = Actor.GetModule<MovementModule>();
        }

        public override void ResetModule()
        {
            base.ResetModule();
            _velocity = Vector3.zero; // clear carried-over velocity when the actor is reused from the pool
        }

        private void HandleMovement()
        {
            if (GameManager.IsGamePaused() || !Actor.IsAlive() || _movementModule == null)
            {
                return;
            }

            Vector3 desiredVelocity = _movementModule.GetMovementVector() * _movementModule.GetSpeed();

            // Frame-rate independent exponential smoothing toward the desired velocity.
            // 1 - e^(-k*dt) gives the same easing regardless of frame rate, and naturally
            // handles smooth stops (desired velocity → zero) and smooth turns.
            float t = 1f - Mathf.Exp(-Acceleration * Time.deltaTime);
            _velocity = Vector3.Lerp(_velocity, desiredVelocity, t);

            Actor.transform.position += _velocity * Time.deltaTime;
        }

        public override void ModuleUpdate()
        {
            HandleMovement();
        }
    }
}
