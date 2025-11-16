using Cysharp.Threading.Tasks;

namespace Kuantech.Core.Controller
{
    public class ControllerManager : SubManager
    {
        //Runtime 
        public PlayerController CurrentController;
        public override async UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);
            
            //Create controller
            CurrentController = new PlayerController();
        }

        public static PlayerController GetCurrentController()
        {
            var ctx = GetContext<ControllerManager>();
            if (ctx == null) return null;
            return ctx.CurrentController;
        }
    }
}