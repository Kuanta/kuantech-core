using UnityEngine;

namespace Kuantech.Core.FX
{
    public class FxBehaviour : MonoBehaviour
    {
        public float Delay;
        
        protected Effect ParentFx;
        private float _fxStartTime;
        private bool _behaviourStarted;
        
        public void StartFxBehaviour(Effect parentFx)
        {
            _fxStartTime = Time.time;
            _behaviourStarted = false;
        }

        public virtual void UpdateFx()
        {
             if(_behaviourStarted) return;
             if(Time.time - _fxStartTime >= Delay)
             {
                 OnFxStarted(ParentFx);
                 _behaviourStarted = true;
             }
        }
        
        protected virtual void OnFxStarted(Effect parentFx)
        {

        }
        
        public virtual void OnFxEnded()
        {
            
        }
    }
}