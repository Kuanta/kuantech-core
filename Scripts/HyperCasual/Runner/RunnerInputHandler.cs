using System;
using Kuantech.Core.UI;
using UnityEngine;

namespace Kuantech.Core.HyperCasual.Runner
{
    public class RunnerInputHandler : MonoBehaviour
    {
        public Runner Runner;
        [SerializeField] private VirtualJoystick VirtualJoystick;
        public bool MovementByDistance = false;

        private void Start()
        {
            VirtualJoystick = FindObjectOfType<VirtualJoystick>();
            if(VirtualJoystick != null)
            {
                VirtualJoystick.OnPointerDownEvent += OnPointerDown;
            }
        }

        private void Update()
        {
            //todo: Check Game State
            if (Runner == null) return;
            if(LevelManager.GetCurrentState() != LevelState.Playing) return;

            float side = 0;
            float forward = 0;

            if (VirtualJoystick != null)
            {
                if (MovementByDistance)
                {
                    side = Mathf.Clamp(VirtualJoystick.GetHorizontalDisplacement(),-1,1);
                }
                else
                {
                    Vector2 joystickInput = VirtualJoystick.GetInputVector();
                    side = Mathf.Clamp(joystickInput.x, -1, 1);
                }
                if (VirtualJoystick.Dragging) forward = 1;
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

        private void OnPointerDown(object sender, EventArgs args)
        {
            if (LevelManager.GetCurrentState() != LevelState.Waiting) return;
            LevelManager.GetContext<LevelManager>().StartLevel();
        }
    }
}