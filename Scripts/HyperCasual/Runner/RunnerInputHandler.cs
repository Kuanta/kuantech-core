using Kuantech.Core.UI;
using UnityEngine;

namespace Kuantech.Core.HyperCasual
{
    public class RunnerInputHandler : MonoBehaviour
    {
        public Runner Runner;
        [SerializeField] private VirtualJoystick VirtualJoystick;
        
        private void Update()
        {
            //todo: Check Game State
            if (Runner == null) return;

            float side = 0;
            float forward = 0;

            if (VirtualJoystick != null)
            {
                Vector2 joystickInput = VirtualJoystick.GetInputVector();
                if (VirtualJoystick.Dragging) forward = 1;
                side = joystickInput.x;
            }

            if (side == 0 && forward == 0)
            {
                if (Input.GetKey(KeyCode.A))
                {
                    side = -1;
                }else if (Input.GetKey(KeyCode.D))
                {
                    side = 1;
                }
                if (Input.GetKey(KeyCode.W))
                {
                    forward = 1;
                }else if (Input.GetKey(KeyCode.S))
                {
                    forward = -1;
                }
            }
            Runner.SetMovementVector(new Vector2(side, forward));
        }
    }
}