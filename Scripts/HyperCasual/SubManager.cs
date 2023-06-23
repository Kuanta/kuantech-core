using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public class SubManager : MonoBehaviour
    {
        protected HCGameManager HcGameManager;
        
        public virtual void Initialize(HCGameManager hcGameManager)
        {
            //Subscribe to events
            HcGameManager = hcGameManager;
            hcGameManager.StateChangeEvent += OnStateChange;
        }

        protected virtual void OnStateChange(object sender, StateChangeData stateChangeData)
        {
            
        }
    }
}