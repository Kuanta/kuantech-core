using Cysharp.Threading.Tasks;

namespace Kuantech.Core.Controller
{
    public class InputManager : SubManager
    {
        public override async UniTask Initialize(GameManager gameManager)
        {
            
        }
        
        private void Update()
        {
            if (!Initialized) return;
            PlayerController controller = ControllerManager.GetCurrentController();
            if (controller == null) return;
            
            //Get yaw
            
            
            //Get pitch
            
        }
    }
}