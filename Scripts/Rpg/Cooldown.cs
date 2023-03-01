using UnityEngine;

namespace Kuantech.Core.Rpg
{
    public class Cooldown
    {
        public float CooldownTime;
        private float _lastCooldown = 0f;

        public Cooldown(float cooldownTime)
        {
            CooldownTime = cooldownTime;
        }
        
        /// <summary>
        /// Starts cooldown 
        /// </summary>
        public void StartCooldown(float cooldownTime)
        {
            CooldownTime = cooldownTime;
            StartCooldown();
        }
        public void StartCooldown()
        {
            _lastCooldown = Time.time;
        }
        //Returns true if is off cooldown
        public bool IsOffCooldown()
        {
            return Time.time - _lastCooldown >= CooldownTime;
        }
        
        /// <summary>
        /// Returns the normalized value of cooldown without clamping.
        /// </summary>
        /// <returns></returns>
        public float GetPercentageCooldown()
        {
            return (Time.time - _lastCooldown) / CooldownTime; 
        }
    }
}