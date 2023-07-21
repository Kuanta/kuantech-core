using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public class SubManager : MonoBehaviour
    {
        protected GameManager ParentManager;
        
        public virtual async UniTask Initialize(GameManager gameManager)
        {
            //Subscribe to events
            ParentManager = gameManager;
        }

        public virtual void OnSubmanagersInitialized()
        {
            
        }
    }
}