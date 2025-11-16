namespace Kuantech.Core.Controller
{
    public class InputManager : SubManager
    {
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