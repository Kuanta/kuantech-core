using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Core
{
    public class InputHandler : MonoBehaviour
    {
        public float SideInput;
        public float ForwardInput;

        public UnityEvent AttackEvent;
        public UnityEvent AimEvent;
        public UnityEvent ReleaseAimEvent;

        public float MovementScale = 1f;
        public bool ReceivedInput;
        protected virtual void Update()
        {
            if (GameManager.Instance.GameIsPaused) return;
            KeyboardInput();
            
        }

        private void KeyboardInput()
        {
            
            //todo(Remove pc controls)
            if (Input.GetKey(KeyCode.A))
            {
                SideInput = -1;
                ReceivedInput = true;
            }
            else if (Input.GetKey(KeyCode.D))
            {
                SideInput = 1;
                ReceivedInput = true;
            }
            else
            {
                SideInput = 0f;
            }
        
            if (Input.GetKey(KeyCode.W))
            {
                ForwardInput = 1;
                ReceivedInput = true;
            }
            else if (Input.GetKey(KeyCode.S))
            {
                ForwardInput = -1;
                ReceivedInput = true;
            }
            else
            {
                ForwardInput = 0f;
            }
            
            if (Input.GetKeyDown(KeyCode.Space))
            {
                AttackEvent?.Invoke();
            }
            
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                MovementScale = 0.5f;
            }else if (Input.GetKeyUp(KeyCode.LeftControl))
            {
                MovementScale = 1f;
            }
        }

        private void JoystickInput()
        {
            
        }
        public void Reset()
        {
            SideInput = 0;
            ForwardInput = 0;
        }
    }
}