using Kuantech.Core.UI;
using UnityEngine;

namespace Kuantech.ArcadeIdle
{
    public class ArcadeIdleInputHandler : MonoBehaviour
    {
        [SerializeField] private VirtualJoystick Joystick;
        
        private float SideInput = 0f;
        private float ForwardInput = 0f;
        
        private void Update()
        {
            if (Input.GetKey(KeyCode.A))
            {
                SideInput = -1;
            }
            else if (Input.GetKey(KeyCode.D))
            {
                SideInput = 1;
            }
            else
            {
                SideInput = 0f;
            }
        
            if (Input.GetKey(KeyCode.W))
            {
                ForwardInput = 1;
            }
            else if (Input.GetKey(KeyCode.S))
            {
                ForwardInput = -1;
            }
            else
            {
                ForwardInput = 0f;
            }

            if (Joystick == null) return;
            Vector2 joystickInput = Joystick.GetInputVector();
            if (joystickInput is {x: 0, y: 0}) return; //Don't kill keyboard input if no input is from joystick
            SideInput = joystickInput.x;
            ForwardInput = joystickInput.y;
        }

        public Vector2 GetLocalInput()
        {
            return new Vector2(SideInput, ForwardInput);
        }
    }
}