using Cysharp.Threading.Tasks;

namespace Kuantech.Core.HyperCasual
{
    public class HCSubManager : SubManager
    {
        public override async UniTask Initialize(GameManager gameManager)
        {
            ((HCGameManager) gameManager).StateChangeEvent += OnStateChange;
            await base.Initialize(gameManager);
        }
        
        protected virtual void OnStateChange(object sender, StateChangeData stateChangeData)
        {
            
        }
    }
}