using System;
using UnityEngine;

namespace Kuantech.Core.Combat
{
    public class ProjectileAttachment : MonoBehaviour {
        protected Projectile AttachedProjectile;
        public bool DestroyOnDespawn = false;
        
        public virtual void OnAttached(Projectile projectile)
        {
            AttachedProjectile = projectile;
        }

        public virtual void OnDetached()
        {
            
        }
    }
}