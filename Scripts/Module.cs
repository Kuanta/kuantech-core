using System;
using UnityEngine;

namespace Kuantech.Core
{   
    public class Module : MonoBehaviour
    {
        public Actor Actor;

        protected virtual void Awake()
        {
            if (Actor == null) return;
            Actor.OnModulesInitialized += OnModulesInitialized;
        }

        public virtual void Initialize()
        {
            
        }
        public virtual void Reset()
        {
            
        }

        public virtual void OnModulesInitialized(object sender, EventArgs args)
        {
            
        }

        public virtual void OnDeath(object sender, EventArgs empty)
        {
            
        }

        public virtual void OnRespawn(object sender, EventArgs empty)
        {
            
        }
    }
}