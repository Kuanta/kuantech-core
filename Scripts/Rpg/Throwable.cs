using Kuantech.Core.Combat;
using Kuantech.Rpg.Inventory;
using UnityEngine;
using UnityEngine.Serialization;

namespace Kuantech.Rpg
{
    public class Throwable : Projectile
    {
        [FormerlySerializedAs("Rigidbody")] public Kuantech.Physics.ThrowableRigidbody throwableRigidbody;

        private float _throwLifetime;
        private bool _thrown;
        private float _thrownTime;
        private float _linearSpeed = 0;
        public delegate void LandDelegate(Throwable throwable);

        public LandDelegate LandHandler;

        /// <summary>
        /// Throws projectile. Total lifetime is calculated from horizontal displacement
        /// </summary>
        /// <param name="combatModule"></param>
        /// <param name="shotFrom"></param>
        /// <param name="horizontalDistance"></param>
        /// <param name="horizontalSpeed"></param>
        /// <param name="direction"></param>
        /// <param name="acceleration"></param>
        /// <param name="initialHeight"></param>
        public void Throw(CombatModule combatModule, Weapon shotFrom, Vector3 shootPosition, Quaternion shootRotation, float horizontalDistance, float horizontalSpeed, Vector2 direction,
            float acceleration = -9.8f, float initialHeight = 0f)
        {
            Initialize(combatModule, shotFrom, shootPosition, shootRotation);
            _throwLifetime = throwableRigidbody.SetTrajectoryWithHorizontalSpeed(horizontalDistance, horizontalSpeed, direction, acceleration, initialHeight);
            _thrownTime = Time.time;
            _thrown = true;
            _linearSpeed = 0;
        }

        public void SetTimeScale(float timeScale)
        {
            throwableRigidbody.TimeScale = timeScale;
            _throwLifetime /= timeScale;
        }
        
        public void SetLifetime(float lifetime)
        {
            _throwLifetime = lifetime;
        }

        public void SetLinearSpeed(float linearSpeed)
        {
            _linearSpeed = linearSpeed;
        }
        protected override void Update()
        {
            if (!_thrown) return;
            if ((Time.time - _thrownTime >= _throwLifetime))
            {
                LandHandler?.Invoke(this);
                Despawn();
                return;
            }

            transform.position += transform.forward * (Time.deltaTime * _linearSpeed);
        }

        public override void Despawn()
        {
            base.Despawn();
            _thrown = false;
        }

        public float GetLifeTime()
        {
            return _throwLifetime;
        }
    }
}