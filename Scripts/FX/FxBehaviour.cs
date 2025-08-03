using UnityEngine;

namespace Kuantech.Core.FX
{
    public class FxBehaviour : MonoBehaviour
    {
        protected Effect ParentFx;
        
        public virtual void OnFxStarted(Effect parentFx)
        {
            ParentFx = parentFx;
            
        }
        
        public virtual void OnFxEnded()
        {
            
        }
    }
}