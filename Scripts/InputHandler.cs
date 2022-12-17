using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Core
{
    public class InputHandler : MonoBehaviour
    {
        public float SideInput;
        public float ForwardInput;

        public UnityEvent<int> AttackEvent;

        private void Update()
        {
            //todo(Remove pc controls)
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
            
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                AttackEvent?.Invoke(0);
            }
        }

        public void Reset()
        {
            SideInput = 0;
            ForwardInput = 0;
        }
    }
}