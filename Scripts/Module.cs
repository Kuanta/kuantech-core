using UnityEngine;

namespace Kuantech.Core
{   
    [RequireComponent(typeof(Actor))]
    public class Module : MonoBehaviour
    {
        public Actor Actor;

        protected virtual void Awake()
        {
            if (Actor == null) Actor = GetComponent<Actor>();
        }

        public virtual void Initialize()
        {
            
        }
        public virtual void Reset()
        {
            
        }
    }
}