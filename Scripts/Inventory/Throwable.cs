using Kuantech.Combat;
using Kuantech.Inventory.Items;
using UnityEngine;
using UnityEngine.SocialPlatforms;

namespace Kuantech.Core.Inventory
{
    public class Throwable : Projectile
    {
        public Kuantech.Physics.Rigidbody Rigidbody;

        private float _throwLifetime;
        private bool _thrown;
        private float _thrownTime;
        
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
        public void Throw(CombatModule combatModule, Weapon shotFrom, float horizontalDistance, float horizontalSpeed, Vector2 direction,
            float acceleration = -9.8f, float initialHeight = 0f)
        {
            Initialize(combatModule, shotFrom);
            _throwLifetime = Rigidbody.SetTrajectoryWithHorizontalSpeed(horizontalDistance, horizontalSpeed, direction, acceleration, initialHeight);
            _thrownTime = Time.time;
            _thrown = true;
        }

        public void SetTimeScale(float timeScale)
        {
            Rigidbody.TimeScale = timeScale;
            _throwLifetime /= timeScale;
        }
        
        public void SetLifetime(float lifetime)
        {
            _throwLifetime = lifetime;
        }
        
        protected override void Update()
        {
            if (!_thrown) return;
            if (!(Time.time - _thrownTime >= _throwLifetime)) return;
            LandHandler?.Invoke(this);
            Despawn();
        }

        public override void Despawn()
        {
            base.Despawn();
            _thrown = false;
        }
    }
}