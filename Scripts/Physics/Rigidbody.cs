using UnityEngine;

namespace Kuantech.Physics
{
    public class Rigidbody : MonoBehaviour
    {
        public Vector3 Velocity;
        public Vector3 Acceleration;

        public bool IsAwake;
        public float TimeScale = 1f;
        private float _lastAwakenTime = 0f;
        private float _secondsToSleep = -1f;
        
        private void FixedUpdate()
        {
            if (!IsAwake) return;
            if (_secondsToSleep >= 0)
            {
                if (Time.time - _lastAwakenTime >= _secondsToSleep / TimeScale)
                {
                    IsAwake = false;
                    return;
                }
            }
            ApplyKinematicEquations(Time.fixedDeltaTime * TimeScale);
            
        }

        private void ApplyKinematicEquations(float deltaTime)
        {
            Vector3 newVelocity = Velocity + Acceleration * deltaTime;
            Vector3 displacement = Velocity * deltaTime + Acceleration * deltaTime * deltaTime * 0.5f;
            transform.position = transform.position + displacement;
            Velocity = newVelocity;
        }
        
        /// <summary>
        /// Sets the initial velocity and acceleration for following a trajectory defined by vertical/horizontal distances and direction.
        /// Returns the required seconds in order to complete the trajectory
        /// </summary>
        /// <param name="verticalDistance"></param>
        /// <param name="horizontalDistance"></param>
        /// <param name="direction">Direction on the horizontal plane</param>
        /// <param name="gravity"></param>
        public float SetTrajectory(float verticalDistance, float horizontalDistance, Vector2 direction, float gravity)
        {
            //Calculate initial vertical velocity
            float verticalSpeedSquared = 2 * Mathf.Abs(gravity) * verticalDistance;
            float verticalSpeed = Mathf.Sqrt(verticalSpeedSquared);
            
            //Calculate time pass
            float time = verticalSpeed / gravity * 2f;
            
            //Calculate vertical speed
            float horizontalSpeed = horizontalDistance / time;

            Velocity = new Vector3(direction.x * horizontalSpeed, verticalSpeed,
                direction.y * horizontalSpeed);
            Acceleration = new Vector3(0f, -Mathf.Abs(gravity), 0f);
            _secondsToSleep = time;
            WakeUp();
            return time;
        }

        public void WakeUp()
        {
            _lastAwakenTime = Time.time;
            IsAwake = true;
        }
    }
}