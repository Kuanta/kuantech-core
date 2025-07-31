using UnityEngine;

namespace Kuantech.Core.FX
{
    public class FxBehaviour : MonoBehaviour
    {
        private Effect _parentFx;
        
        public virtual void OnFxStarted(Effect parentFx)
        {
            _parentFx = parentFx;
            
        }

        public virtual void OnFxEnded()
        {
            
        }
    }
}