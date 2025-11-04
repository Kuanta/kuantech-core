using Kuantech.Rpg.Inventory;
using UnityEngine;

namespace Kuantech.Core
{
    public class PiercingBullet : Projectile
    {
        public int PiercingCount = 1; //How many targets can this bullet pierce through
        
        private int _remainingPiercing;
        public override void Shoot(Actor castBy, Weapon shotFrom, Vector3 shootPosition, Vector3 shootDirection,
            Transform target = null, float relativeSpeed = 0.0f)
        {
            base.Shoot(castBy, shotFrom, shootPosition, shootDirection, target, relativeSpeed);
            
            //Set piercing count
            _remainingPiercing = PiercingCount;
        }

        protected override void Impact(GameObject impacted)
        {
            base.Impact(impacted);

            _remainingPiercing--;
            
            //If targeted, remove target and remain on the last direction
            Target = null;
        }

        protected override void CheckDespawn()
        {
            if (_remainingPiercing >= 0) return;
            base.CheckDespawn();
        }
    }
}