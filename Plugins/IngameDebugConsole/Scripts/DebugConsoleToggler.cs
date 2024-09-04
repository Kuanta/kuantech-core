using UnityEngine;

namespace IngameDebugConsole
{
    public class DebugConsoleToggler : MonoBehaviour
    {
        [SerializeField] private DebugLogManager Console;
        
        public float doubleTapTime = 0.3f;  // Max time between taps for it to be considered a double-tap
        public float holdDuration = 1.0f;   // Time to hold after the second tap to trigger the debug console

        private float lastTapTime;
        private bool isDoubleTap;
        private bool isHolding;
        private float holdStartTime;
        
        private void Update()
        {
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Began)
                {
                    float timeSinceLastTap = Time.time - lastTapTime;

                    if (timeSinceLastTap <= doubleTapTime)
                    {
                        isDoubleTap = true;
                        holdStartTime = Time.time;
                    }
                    else
                    {
                        isDoubleTap = false;
                    }

                    lastTapTime = Time.time;
                }
                else if (touch.phase == TouchPhase.Stationary && isDoubleTap)
                {
                    isHolding = true;

                    if (Time.time - holdStartTime >= holdDuration)
                    {
                        ToggleConsole();
                        ResetGesture();
                    }
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    ResetGesture();
                }
            }
            else if (Input.touchCount == 0 && isHolding)
            {
                ResetGesture();
            }
        }
        
        public void ToggleConsole()
        {
            bool toggle = !Console.isActiveAndEnabled;
            if (Console == null) return;
            if (toggle)
            {
                Console.ShowLogWindow();
            }
            else
            {
                Console.HideLogWindow();
            }
        }
        
        void ResetGesture()
        {
            isDoubleTap = false;
            isHolding = false;
        }
    }
}